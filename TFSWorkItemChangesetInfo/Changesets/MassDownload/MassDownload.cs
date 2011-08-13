using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IntraApplicationLogService;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSCommon;
using TFSWorkItemChangesetInfo.Database;
using TFSWorkItemChangesetInfo.Extensions.Microsoft.TeamFoundation.WorkItemTracking.Client_;
using TFSWorkItemChangesetInfo.Extensions.System_;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    /// <summary>
    /// TODO: Break this messy class up / refactor when there is more time
    /// </summary>
    public partial class MassDownload : IMassDownload, IDisposable
    {
        public MassDownload()
        {
            OutstandingOperations = 0;
            this.LogBuilder = new StringBuilder();
            _taskChanges = new List<WorkItemResult>();
            this.DatabaseHandler = new DatabaseChangeHandler(this);
        }

        private volatile int _outstandingOperations;
        public int OutstandingOperations
        {
            get
            {
                //counterLock.EnterReadLock();
                var tmp = _outstandingOperations;
                //counterLock.ExitReadLock();
                return tmp;
            }

            private set
            {
                //counterLock.EnterWriteLock();
                _outstandingOperations = value;
                //counterLock.ExitWriteLock();
            }
        }

        private void Log(string text, params object[] args)
        {
            var msg = string.Format(text, args);
            //TODO: logging such as log4net etc.
            LogBuilder.AppendLine(msg);
        }

        private StringBuilder LogBuilder { get; set; }
        private TeamFoundationServer TfsServer { get; set; }  // obsolete
        private VersionControlServer VersionControl { get; set; }
        private Utility TfsUtility { get; set; }

        private DatabaseChangeHandler DatabaseHandler { get; set; }

        public string DownloadPath
        {
            get { return this.Config.DownloadPath; }
        }

        public string LogText
        {
            get { return (null != this.LogBuilder) ? this.LogBuilder.ToString() : null; }
        }

        public Action<MassDownloadProgress> ProgressAction;

        private string Stage { get; set; }

        public void CancelDownload()
        {
            this.CancelPending = true;
        }

        private bool CancelPending { get; set; }

        private void ReportProgress(MassDownloadProgress p, bool log = false)
        {
            p.Stage = this.Stage;
            if (null != ProgressAction)
                ProgressAction(p);

            if (log)
                Log(p.Message);

            if (p.Cancel)
                this.CancelDownload();
        }

        private void ReportProgress(string msg, bool log = false)
        {
            ReportProgress(new MassDownloadProgress {Message = msg}, log);
        }

        private void ReportProgress(string msg, WorkItem wi, bool log = false)
        {
            var assignedTo = wi.GetAssignedTo();
            ReportProgress(new MassDownloadProgress
            {
                Message = msg, ItemTitle = wi.Title, ItemType = wi.Type.Name, ItemBy = assignedTo, ItemId = wi.Id
            }, log);
        }

        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        public void DownloadChanges(MassDownloadConfig configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (null == configuration.KnownFileTypes)
                throw new NullReferenceException("Config KnownFileTypes must be set");

            if (string.IsNullOrWhiteSpace(configuration.TfsServer))
                throw new NullReferenceException("TfsServer must be set in configuration");

            this.Stage = "Initializing";
            this.StartTime = DateTime.Now;
            this.Config = configuration;
            this.Config.DownloadPath = DirUtility.EnsureDir(@configuration.BaseDownloadDirectory, 
                string.Format(@"{0}\{1}\WI {2}\{3}", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"),
                this.Config.ProjectId, DateTime.Now.ToString("dd ddd HH-mm-ss")));

            this.Config.AttachmentPath = DirUtility.EnsureDir(this.DownloadPath, "Attachments");
            var changesetList = GetChangeSetListAndAttachments();

            if (!PendingCancellation())
            {
                this.Stage = "Analyzing, downloading, organizing changesets";
                var index = 0;
                var csList = changesetList.OrderBy(cs => cs.CreationDate).ToList();
                csList.ForEach(cs => ProcessChangeSet(cs, ++index, csList.Count()));
            }

            if (!PendingCancellation())
            {
                this.Stage = "Post-processing";
                DatabasePostProcess();
                OutputTaskInfo();
            }

            this.Stage = "Cleaning up";
            ReportProgress("Tearing down");
            MassDownloadCleanup();
            this.EndTime = DateTime.Now;
            var ts = this.EndTime.Value - this.StartTime.Value;
            Log("Mass download complete in {0}", ts);

            SaveLog();

            if (!PendingCancellation())
                Process.Start(this.DownloadPath);

            this.CancelPending = false;
        }

        private void SaveLog()
        {
            ReportProgress("Saving log");
            var filename = Path.Combine(this.DownloadPath, "Log.txt");
            File.WriteAllText(filename, this.LogText);
        }

        private void ProcessChangeSet(Changeset changeset, int index, int count)
        {
            if (PendingCancellation()) return;

            Log("Changeset ID: {0} Creation Date: {1}", changeset.ChangesetId, changeset.CreationDate);

            if (!changeset.Changes.Any())
            {
                Log("\tNo Changes.");
                return;
            }

            var changesetBy = changeset.Owner.RemoveAfterLast(@"\");
            var msg = string.Format(
                "Processing changeset {0} of {1} created by {2} on {3}", index, count, changesetBy, changeset.CreationDate);
            ReportProgress(new MassDownloadProgress
                {
                    ItemType = "Changeset",
                    //ItemTitle = string.Format("Changeset {0}", changeset.ChangesetId),
                    ItemBy = changesetBy,
                    ItemId = changeset.ChangesetId,
                    Message = msg
                });
            
            changeset.Changes.ToList().ForEach(c=> ProcessChange(changeset, c));
        }

        private bool PendingCancellation()
        {
            if (CancelPending)
            {
                if (!_cancelLogged)
                {
                    Log("Cancelling download");
                    _cancelLogged = true;
                }

                return true;
            }
            return false;
        }

        private bool _cancelLogged;

        private bool ShouldIncludeChange(Change change)
        {
            if (PendingCancellation()) return false;

            if (this.Config.HasSourceControlExclusions &&
                this.Config.SourceControlExclusions.Any(an => change.Item.ServerItem.Contains(an)))
            {
                Log(
                    "\t{0} not evaluated because its source control path was marked as excluded.",
                    change.Item.ServerItem);
                return false;
            }

            Log("\t{0} ({1})", change.Item.ServerItem, change.ChangeType.ToString());

            if (change.Item.ItemType != ItemType.File)
            {
                Log("\t[{0} was {1} and wasn't downloaded.]", change.Item.ServerItem, change.Item.ItemType.ToString());
                return false;
            }

            return true;
        }
        
        private void ProcessChange(Changeset changeset, Change change)
        {
            if (!ShouldIncludeChange(change)) return;

            var info = GetChangeInfo(changeset, change);
                    
            if (info.HasIncompleteTask)
            {
                var sb = new StringBuilder();
                sb.AppendFormat(
                    "One or more incomplete tasks associated with changeset {0}:{1}", changeset.ChangesetId,
                    Environment.NewLine);
                info.Tasks.ForEach(t=> sb.AppendFormat("{0} [{1}] : {2}{3}", t.Id, t.State, t.Title, Environment.NewLine));
                Log(sb.ToString());
            }
            
            if (change.ChangeType != ChangeType.Delete)
            {
                Log("Downloading {0}", info.TargetFilename);
                change.Item.DownloadFile(info.TargetFilename);

                if (info.IsDatabase)
                {
                    ChangeSetFileTagger.Tag(changeset, change, info.TargetFilename);
                }
            }
            else
            {
                if (File.Exists(info.TargetFilenameDeleted))
                    File.Move(info.TargetFilenameDeleted, info.TargetFilename);

                var infoFilename = string.Format("{0}.deletedInfo", info.TargetFilename);
                if (!File.Exists(infoFilename))
                {
                    using (var fs = new FileStream(infoFilename, FileMode.Create))
                    {
                        fs.Close();
                    }
                }
                ChangeSetFileTagger.Tag(changeset, change, infoFilename);
            }
            
            if (info.IsDatabase)
            {
                this.DatabaseHandler.HandleDatabaseChange(info);
            }
            
            info.TaskChanges.ToList().ForEach(tc=> tc.AddTaskFile(info.TargetFilename, this.DownloadPath, change.Item.ServerItem, 
                change.ChangeType == ChangeType.Delete));
        }

        private IEnumerable<Changeset> GetChangeSetListAndAttachments()
        {
            ReportProgress("Connecting. Locating the monkeys", log:true);
            this.TfsServer = TFSCommon.Server.GetTfsServer(this.Config.TfsServer);
            this.VersionControl = (VersionControlServer)this.TfsServer.GetService(typeof(VersionControlServer));
            var changesetList = new List<Changeset>();

            this.TfsUtility = new Utility(this.TfsServer);

            this.Stage = "Walking related TFS work items";
            
            // consider making the match predicate for get related work items non-hardcoded
            Predicate<WorkItem> pred =
                xxx => xxx.Type.Name == "Bug" || xxx.Type.Name == "Scenario" || xxx.Type.Name == "Issue";
            // since we don't typically link tasks to product backlog items, we'd have to get scenarios or bugs off Product Backlog Item
            // and then get tasks off that so that introduces another level that we are not ready to deal with at the moment.
            //|| xxx.Type.Name == "Product Backlog Item";
            ReportProgress(string.Format("Getting related work items for work item {0}", this.Config.ProjectId), log:true);
            var items = this.TfsUtility.GetRelatedWorkItems(this.Config.ProjectId, pred).ToList();

            Log("Found {0} related work items for work item {1}", items.Count, this.Config.ProjectId);

            foreach (var item in items)
            {
                var taskChanges = ProcessWorkItem(item);
                if (PendingCancellation()) break;

                changesetList.AddRange(taskChanges.SelectMany(x=> x.TaskChangeSets));
                _taskChanges.AddRange(taskChanges);
            }

            return changesetList;
        }

        private List<WorkItemResult> ProcessWorkItem(WorkItem item)
        {
            var results = new List<WorkItemResult>();
            if (PendingCancellation()) return results;

            ReportProgress("Getting related tasks, changeset list, and attachments", item);
            Log("Getting related tasks for work item {0}", item.Id);

            var tasks = this.TfsUtility.GetRelatedWorkItems(item.Id, ch => ch.Type.Name == "Task");
            if (tasks == null)
            {
                Log("No related tasks found for work item {0}", item.Id);
                return results;
            }

            var nonProcessedTasks = tasks.Where(t => !_taskChanges.Any(x => x.Task.Id == t.Id)).ToList();
            Log("Processing {0} non-processed tasks", nonProcessedTasks.Count);
            
            // might need to be able to selectively skip tasks at some point
            foreach (var task in nonProcessedTasks)
            {
                var result = new WorkItemResult {Task = task};
                var extLinks = task.Links.OfType<ExternalLink>();
                Log("Getting changeset list for task {0}", task.Id);
                result.TaskChangeSets.AddRange(extLinks.Select(extLink => this.VersionControl.ArtifactProvider.GetChangeset(new Uri(extLink.LinkedArtifactUri))));

                results.Add(result);

                if (task.Attachments.Count <= 0) continue;
                Log("Getting {0} attachments for task {1}", task.Attachments.Count, task.Id);

                foreach (Attachment attachment in task.Attachments)
                {
                    var attachment1 = attachment;
                    var task1 = task;
                    Task.Factory.StartNew(() => GetAttachment(attachment1.Uri, attachment1.Name, this.Config.AttachmentPath, task1.Id));
                }
            }

            return results;
        }

        private void GetAttachment(Uri uri, string attachmentName, string rootDownloadPath, int workItemId)
        {
            Log("Downloading attachment '{0}' of work item {1}", attachmentName, workItemId);
            var downloadPath = DirUtility.EnsureDir(rootDownloadPath, "TFS-" + workItemId.ToString());

            using (var webClient = new WebClient())
            {
                webClient.UseDefaultCredentials = true;
                webClient.DownloadFileCompleted += (s, e) =>
                {
                    Action<string, object> logAction = (msg, f) => Log(msg, f);

                    if (e.Error == null)
                        logAction.BeginInvoke("File {0} downloaded.", e.UserState.ToString(), CallBack, logAction);
                    else
                        logAction.BeginInvoke("Error: {0}", LogService.ExceptionFormatter(e.Error), CallBack, logAction);
                };

                webClient.DownloadProgressChanged += (s, e) => Log("{0} Progress Percentage: {1}", e.UserState, e.ProgressPercentage);
                var fileName = Path.Combine(downloadPath, attachmentName);
                OutstandingOperations++;
                webClient.DownloadFileAsync(uri, fileName, fileName);
            }
        }

        private void CallBack(IAsyncResult ar)
        {
            var t = (Action<string, object>)ar.AsyncState;
            t.EndInvoke(ar);

            OutstandingOperations--;
            Log("Outstanding operations: {0}", OutstandingOperations);
        }

        public DatabaseChanges DatabaseChanges
        {
            get { return this.DatabaseHandler.DatabaseChanges; }
        }

        private void OutputTaskInfo()
        {
            this.ReportProgress("Gathering task information", log:true);
            var gen = new TaskInfoGenerator(_taskChanges, this.DownloadPath);
            gen.Generate();
        }

        private void DatabasePostProcess()
        {
            ReportProgress("Combining, organizing database scripts", log:true);
            DatabaseHandler.PostProcess();
        }

        #region IDisposable Members

        public void Dispose()
        {
            MassDownloadCleanup();
        }

        private void MassDownloadCleanup()
        {
            if (null != this.TfsServer)
                this.TfsServer.Dispose();

            if (null != this.TfsUtility)
                this.TfsUtility.Dispose();

            if (null != _taskChanges)
            {
                _taskChanges.Clear();
            }
        }

        #endregion IDisposable Members


        #region IMassDownload Members

        public MassDownloadConfig Config { get; private set; }

        private readonly List<WorkItemResult> _taskChanges;
            
        List<WorkItemResult> IMassDownload.TaskChanges
        {
            get { return _taskChanges; }
        }

        #endregion
    }
    
}
