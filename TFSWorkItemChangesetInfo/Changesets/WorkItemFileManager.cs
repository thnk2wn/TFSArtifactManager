using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets
{
    public class WorkItemFileManager
    {
        internal WorkItemFileManager(KnownFileTypes knownFileTypes)
        {
            this.KnownFileTypes = knownFileTypes;
            this.DeployInfo = new WorkItemDeployInfo();
            this.DbFileOrderGuess = new Dictionary<string, int>();
        }

        internal WorkItem WorkItem { get; set; }
        internal IEnumerable<Changeset> Changesets { get; set; }

        public KnownFileTypes KnownFileTypes { get; private set; }
        public WorkItemDeployInfo DeployInfo { get; private set; }

        public string BaseOutputDirectory { get; private set; }
        public string WorkItemDirectory { get; private set; }
        public string ChangesDirectory { get; private set; }

        private Dictionary<string, int> DbFileOrderGuess { get; set; }

        /// <summary>
        /// Downloads changesets and work item attachments
        /// </summary>
        /// <param name="baseOutputDirectory">If baseOutputDirectory is not specified, Environment.CurrentDirectory is used</param>
        public WorkItemDeployInfo DownloadAllFiles(string baseOutputDirectory = null)
        {
            SetBaseDirectories(baseOutputDirectory);
            DownloadChangeSets();
            DownloadWorkItemAttachments();
            CreateWorkItemFile();
            CreateChangeInfoFile();
            ConcatDatabaseFiles();
            StampFileOrderGuess();

            return this.DeployInfo;
        }

        private void StampFileOrderGuess()
        {
            foreach (var de in this.DbFileOrderGuess)
            {
                var fiOrig = new FileInfo(de.Key);
                var newFilename = Path.Combine(fiOrig.Directory.FullName, de.Value.ToString("000") + "_" + fiOrig.Name);
                if (File.Exists(newFilename))
                    File.Delete(newFilename);

                File.Copy(fiOrig.FullName, newFilename);
                File.Delete(fiOrig.FullName);
            }
        }

        private void ConcatDatabaseFiles()
        {
            var sb = new StringBuilder();

            var divLine = new string('=', 120);
            var dbChanges = this.DeployInfo.DatabaseChanges.ToList();

            var sbDrop = new StringBuilder();

            if (dbChanges.Any())
            {
                sbDrop.AppendLine();
                sbDrop.AppendLine("-- WARNING: Dropping objects will result in loss of grants.");
            }

            dbChanges.ForEach(c=>
                {
                    sb.AppendLine("/*");
                    sb.AppendLine(divLine);

                    sb.AppendFormat("Server Path: {0}{1}", c.ServerPath, Environment.NewLine);
                    sb.AppendFormat("Last Changed by {0} on {1}{2}", c.LastChangedBy, c.LastChangedDate, Environment.NewLine);
                    if (!string.IsNullOrWhiteSpace(c.CombinedCheckInComments))
                    {
                        sb.AppendLine("Combined CheckIn Comments:");
                        sb.Append(c.CombinedCheckInComments);
                    }
                    sb.AppendLine(divLine);
                    sb.AppendLine("*/");

                    var dbObjectName = c.ItemName.ToUpper();
                    sbDrop.AppendFormat("DROP {0} {1};{2}{2}", c.FileExt.Name, dbObjectName, Environment.NewLine);

                    sb.Append(File.ReadAllText(c.LocalFilename));
                    
                    sb.AppendLine();
                    sb.AppendLine();
                });

            if (dbChanges.Any() && sb.Length > 0)
            {
                var dbTypeName = this.KnownFileTypes.DatabaseFileTypes.TypeName;
                var dbPath = Path.Combine(this.ChangesDirectory, dbTypeName);
                var file = Path.Combine(dbPath, string.Format("TFS_{0}_Combined.sql", this.WorkItem.Id));
                File.WriteAllText(file, sb.ToString());

                file = Path.Combine(dbPath, string.Format("TFS_{0}_DROP.sql", this.WorkItem.Id));
                File.WriteAllText(file, sbDrop.ToString());
            }
        }


        private void DownloadChangeSets()
        {
            EnsureTfsItems();
            this.Changesets.OrderBy(x => x.CreationDate).ToList().ForEach(DownloadChangeSet);
        }

        private void DownloadWorkItemAttachments()
        {
            if (null == this.WorkItem) throw new NullReferenceException("WorkItem must be set");
            if (this.WorkItem.AttachedFileCount <= 0) return;

            var attachDir = DirUtility.EnsureDir(this.WorkItemDirectory, "Attachments");
            
            using (var webClient = new WebClient())
            {
                webClient.UseDefaultCredentials = true;
                foreach (Attachment attach in this.WorkItem.Attachments)
                {
                    webClient.DownloadFile(attach.Uri, Path.Combine(attachDir, attach.Name));
                }
            }
        }

        public void OpenChangesDirectory()
        {
            OpenDirectory("Changes", this.ChangesDirectory);
        }

        public void OpenWorkItemDirectory()
        {
            OpenDirectory("Work Item", this.WorkItemDirectory);
        }
        
        // --------------------------------------------------------------------
        // PRIVATE METHODS
        // --------------------------------------------------------------------

        private void CreateWorkItemFile()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Id: {0}{1}", this.WorkItem.Id, Environment.NewLine);
            sb.AppendFormat("Title: {0}{1}", this.WorkItem.Title, Environment.NewLine);
            sb.AppendFormat("Type: {0}{1}", this.WorkItem.Type.Description, Environment.NewLine);
            sb.AppendFormat("State: {0} ({1}){2}", this.WorkItem.State, this.WorkItem.Reason, Environment.NewLine);
            sb.AppendFormat("Area: {0}{1}", this.WorkItem.AreaPath, Environment.NewLine);
            sb.AppendFormat("Assigned To: {0}{1}", WorkItem.Fields["Assigned To"].Value, Environment.NewLine);
            sb.AppendFormat("Created On: {0}{1}", WorkItem.CreatedDate, Environment.NewLine);
            sb.AppendFormat("Changed On: {0}{1}", WorkItem.ChangedDate, Environment.NewLine);
            sb.AppendLine();
            sb.AppendLine("Description:");
            sb.AppendLine(this.WorkItem.Description);
            
            var filename = Path.Combine(this.WorkItemDirectory, "WorkItemInfo.txt");
            File.WriteAllText(filename, sb.ToString());
        }

        private void CreateChangeInfoFile()
        {
            var csList = this.Changesets.OrderBy(cs=> cs.ChangesetId).ToList();
            var sb = new StringBuilder();
            var csIds = string.Empty;

            csList.ForEach(cs=>
                {
                    sb.AppendLine(new String('=', 120));
                    csIds += cs.ChangesetId + ", ";
                    sb.AppendFormat("Id: {0}{1}", cs.ChangesetId, Environment.NewLine);
                    sb.AppendFormat("Change Count: {0}{1}", cs.Changes.Count(), Environment.NewLine);

                    cs.Changes.ToList().ForEach(c=>
                        {
                            sb.AppendFormat("\t{0} - {1}", c.ChangeType, c.Item.ServerItem);
                            sb.AppendLine();
                        });
                    sb.AppendLine();

                    sb.AppendFormat("Committer: {0}{1}", cs.Committer, Environment.NewLine);
                    sb.AppendFormat("Owner: {0}{1}", cs.Owner, Environment.NewLine);
                    sb.AppendFormat("Created On: {0}{1}", cs.CreationDate, Environment.NewLine);
                    sb.AppendFormat("Comment: {0}{1}{0}{0}", Environment.NewLine, cs.Comment);
                    // checkin note is not useful
                });

            var sbReport = new StringBuilder();
            sbReport.AppendFormat("Changeset Count: {0}{1}", csList.Count, Environment.NewLine);
            sbReport.AppendFormat("Changeset Ids: {0}{1}", csIds.Substring(0, csIds.Length-2), Environment.NewLine);
            sbReport.AppendLine();
            sbReport.Append(sb);

            var filename = Path.Combine(this.ChangesDirectory, "ChangesetInfo.txt");
            File.WriteAllText(filename, sbReport.ToString());
        }

        private static void OpenDirectory(string dirLabel, string dir)
        {
            if (string.IsNullOrWhiteSpace(dirLabel))
                throw new ArgumentNullException("dirLabel", "dirLabel is required");

            if (string.IsNullOrWhiteSpace(dir))
                throw new InvalidOperationException(string.Format("{0} directory is unknown / not set", dirLabel));

            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException(string.Format("{0} directory not found: {1}", dirLabel, dir));

            Process.Start(dir);
        }

        private void SetBaseDirectories(string baseOutputDirectory)
        {
            SetBaseDirectory(baseOutputDirectory);

            this.WorkItemDirectory = GetWorkItemDir();
            this.ChangesDirectory = Path.Combine(this.WorkItemDirectory, "Changes");
            this.DeployInfo.WorkItemDirectory = this.WorkItemDirectory;
        }

        private void EnsureTfsItems()
        {
            if (null == this.Changesets || null == this.WorkItem || 0 == this.WorkItem.Id)
                throw new InvalidOperationException("Both changesets and work item must be set to download changes");
        }

        private void SetBaseDirectory(string baseOutputDirectory)
        {
            var currentDirectory = Environment.CurrentDirectory;

#if DEBUG
            if (Debugger.IsAttached)
                currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
#endif

            var baseDir = string.IsNullOrWhiteSpace(baseOutputDirectory)
                              ? currentDirectory
                              : baseOutputDirectory;

            DirUtility.EnsureDir(baseDir);
            this.BaseOutputDirectory = baseDir;
        }

        private string GetWorkItemDir()
        {
            var dir = Path.Combine(this.BaseOutputDirectory, 
                string.Format(@"{0}\{0}-{1}\WI {2}", DateTime.Now.Year, DateTime.Now.Month.ToString("00"), this.WorkItem.Id));

            // we might pickup something leftover from a previous run that is no longer valid
            if (Directory.Exists(dir))
                DirUtility.DeleteDirectory(dir);

            DirUtility.EnsureDir(dir);
            return dir;
        }

        private void DownloadChangeSet(Changeset changeSet)
        {
            changeSet.Changes.ToList().ForEach(c => DownloadChange(c, changeSet));
        }

        private void DownloadChange(Change change, Changeset cs)
        {
            try
            {
                if (change.Item.ItemType == ItemType.File)
                {
                    var file = change.Item.ServerItem.Split('/').Last();
                    var masterFilename = Path.Combine(this.ChangesDirectory, file);
                    var fileType = this.KnownFileTypes.GetTypeForFilenameExt(masterFilename);
                    var masterDir = Path.Combine(this.ChangesDirectory, fileType.TypeName);
                    masterFilename = Path.Combine(masterDir, file);

                    if (change.ChangeType != ChangeType.Delete)
                    {
                        change.Item.DownloadFile(masterFilename);
                        this.DeployInfo.AddChange(change, cs, masterFilename, fileType);

                        if (fileType.FileType == KnownFileType.Database)
                        {
                            if (!this.DbFileOrderGuess.ContainsKey(masterFilename))
                                this.DbFileOrderGuess.Add(masterFilename, this.DbFileOrderGuess.Count + 1);
                        }
                    }
                    else
                    {
                        if (File.Exists(masterFilename))
                        {
                            var newMasterFilename = masterFilename + ".deleted";
                            File.Move(masterFilename, newMasterFilename);
                            this.DeployInfo.AddChange(change, cs, newMasterFilename, fileType);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                const string errorDirName = "wacko";
                var errorDirLocation = string.Format(
                    @"{0}\{1}",
                    this.WorkItemDirectory, errorDirName);

                DirUtility.EnsureDir(errorDirLocation);

                var fs =
                    File.Create(
                        string.Format(
                            @"{0}\error_{1}_{2}.txt", errorDirLocation, cs.ChangesetId,
                            DateTime.Now.ToString("yyyyMMddHHmmssfff")));

                var text = Encoding.UTF8.GetBytes(
                    string.Format(
                        "Message: {0}\r\nStack Trace: {1}", exception.Message, exception.StackTrace)
                    );

                fs.Write(text, 0, text.Length);

                fs.Close();
            }
        }
    }
}
