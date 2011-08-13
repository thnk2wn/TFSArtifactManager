using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSWorkItemChangesetInfo.Database;
using TFSWorkItemChangesetInfo.Extensions.Microsoft.TeamFoundation.WorkItemTracking.Client_;
using TFSWorkItemChangesetInfo.IO;

namespace TFSWorkItemChangesetInfo.Changesets.MassDownload
{
    internal class DatabaseChangeHandler
    {
        private readonly List<DatabaseChange> _databaseChanges = new List<DatabaseChange>();
        private readonly List<string> _databasePaths = new List<string>();

        private string RootDatabaseFolder
        {
            get { return this.MassDownload.Config.RootDatabasePath; }
        }

        public DatabaseChanges DatabaseChanges { get; private set; }

        public DatabaseChangeHandler(IMassDownload massDownload)
        {
            this.MassDownload = massDownload;
        }

        private IMassDownload MassDownload { get; set; }

        public void HandleDatabaseChange(MassDownloadChangeInfo info)
        {
            // incomplete task handling?
            // database paths will be used for combining sql scripts. don't want deleted paths in there or incomplete
            if (info.IsDatabase && !info.HasIncompleteTask && !info.IsDeleted && !_databasePaths.Contains(info.TargetDirectory))
                _databasePaths.Add(info.TargetDirectory);

            var dbChange = _databaseChanges.Where(x => x.Filename == info.TargetFilename).FirstOrDefault() ??
                new DatabaseChange
                {
                    Schema = info.DatabaseSchema,
                    Extension = info.Extension,
                    Filename = info.TargetFilename,
                    ServerItem = info.ServerItem
                };

            info.Tasks.ForEach(t => dbChange.AddTask(
                    new TaskInfo { Id = t.Id, Title = t.Title, State = t.State, AssignedTo = t.GetAssignedTo() }));

            // change.Item.CheckinDate is DateTime.MinValue
            var checkinDate = info.Changeset.CreationDate;

            if (null == dbChange.FirstChanged)
            {
                dbChange.FirstChanged = checkinDate;
            }

            if (dbChange.LastChanged == null || checkinDate > dbChange.LastChanged)
                dbChange.LastChanged = checkinDate;

            dbChange.AddChangeType((ChangeTypes)(int)info.Change.ChangeType);

            if (!_databaseChanges.Any(x => x.Filename == info.TargetFilename))
            {
                var replacePath = this.RootDatabaseFolder + @"\";
                dbChange.FilePath = dbChange.Filename.Replace(replacePath, string.Empty);
                _databaseChanges.Add(dbChange);
            }
        }

        public void PostProcess()
        {
            // combine database files per schema object type
            _databasePaths.ForEach(CombineDbFilesInDir);
            HandleDatabaseTaskAttachments();

            this.DatabaseChanges = new DatabaseChanges
            {
                RootDatabaseFolder = this.RootDatabaseFolder,
                RootWorkItemId = this.MassDownload.Config.ProjectId,
                ExcludedChanges = new ObservableCollection<DatabaseChange>(_databaseChanges),
                GeneratedAt = DateTime.Now,
                DeletedSubDirName = Constants.DELETED_SUB_DIR_NAME
                //ExcludedChanges = new ObservableCollection<DatabaseChange>(_databaseChanges.Where(x=> !x.IsDeleted && !x.IsIncomplete)),
                //IncompleteChanges = new ObservableCollection<DatabaseChange>(_databaseChanges.Where(x=> x.IsIncomplete)),
                //DeletedChanges = new ObservableCollection<DatabaseChange>(_databaseChanges.Where(x=> x.IsDeleted))
            };
        }

        private void CombineDbFilesInDir(string path)
        {
            var di = new DirectoryInfo(path);
            var schema = di.Parent.Name;

            var files = di.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(
                f => !f.Extension.StartsWith(".deleted")).ToList();

            if (files.Count < 2) return;

            const string combineSuffix = ".combined.sql";
            var combinedFilename = Path.Combine(path, string.Format("_{0}.{1} ({2}){3}", schema, di.Name, files.Count, combineSuffix));

            foreach (var fi in files)
            {
                if (fi.FullName.EndsWith(combineSuffix)) continue;

                var contents = File.ReadAllText(fi.FullName);
                File.AppendAllText(combinedFilename, contents + Environment.NewLine + Environment.NewLine);

                if (!_databaseChanges.Any(x => x.Filename == combinedFilename))
                {
                    var dbChange = new DatabaseChange { Filename = combinedFilename, Schema = schema };
                    dbChange.AddChangeType(ChangeTypes.Add);
                    var fileType = this.MassDownload.Config.KnownFileTypes.GetTypeForFilenameExt(combinedFilename);
                    dbChange.FilePath = combinedFilename.Replace(this.RootDatabaseFolder + @"\", string.Empty);
                    dbChange.Extension = fileType.GetFileExtension(".sql");
                    dbChange.FirstChanged = DateTime.Now;
                    dbChange.LastChanged = dbChange.FirstChanged;
                    _databaseChanges.Add(dbChange);
                }
            }
        }

        private void HandleDatabaseTaskAttachments()
        {
            var attachDir = new DirectoryInfo(this.MassDownload.Config.AttachmentPath);
            var files = attachDir.GetFiles("*.sql", SearchOption.AllDirectories).ToList();
            if (!files.Any() || string.IsNullOrEmpty(this.RootDatabaseFolder)) return;

            var attachSqlDir = DirUtility.EnsureDir(this.RootDatabaseFolder, "_Attachments");
            files.ForEach(f => HandleDatabaseTaskAttachment(f, attachSqlDir));
        }

        private void HandleDatabaseTaskAttachment(FileInfo fiAttach, string attachSqlDir)
        {
            var workItemId = fiAttach.Directory.Name.Replace("TFS-", string.Empty); // ick
            var targetFilename = Path.Combine(attachSqlDir, "TFS-" + workItemId + "_" + fiAttach.Name);
            File.Copy(fiAttach.FullName, targetFilename);

            var result = this.MassDownload.TaskChanges.Where(x => x.Task.Id == Convert.ToInt32(workItemId)).FirstOrDefault();

            if (null == result) return;
            result.AddTaskFile(targetFilename, this.MassDownload.Config.DownloadPath, "[Task Attachment]", isDelete: false);

            if (null == _databaseChanges || _databaseChanges.Any(x => x.Filename == targetFilename)) return;

            var fileType = this.MassDownload.Config.KnownFileTypes.GetTypeForFilenameExt(targetFilename);
            var ext = fileType.GetFileExtensionForFile(targetFilename);
            var file = targetFilename.Replace(this.RootDatabaseFolder + @"\", string.Empty);

            var change = new DatabaseChange
            {
                Extension = ext,
                Filename = targetFilename,
                FilePath = file,
                Schema = null, // indeterminate really
                IsAttachment = true
            };

            change.AddChangeType(ChangeTypes.Add);

            var task = result.Task;
            var taskAttach = task.Attachments.OfType<Attachment>().Where(x => x.Name == fiAttach.Name).FirstOrDefault();

            if (null != taskAttach)
            {
                change.FirstChanged = taskAttach.CreationTime;
                change.LastChanged = taskAttach.LastWriteTime;
            }

            change.AddTask(new TaskInfo
            {
                Id = task.Id,
                State = task.State,
                Title = task.Title,
                AssignedTo = task.GetAssignedTo()
            });

            _databaseChanges.Add(change);
        }
    }
}
