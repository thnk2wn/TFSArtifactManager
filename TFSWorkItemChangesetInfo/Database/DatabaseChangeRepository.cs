using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TFSWorkItemChangesetInfo.IO;
using TFSWorkItemChangesetInfo.WorkItems;

namespace TFSWorkItemChangesetInfo.Database
{
    /// <summary>
    /// Loads / saves database changes to an xml file
    /// </summary>
    /// <remarks>
    /// We could just use xml or binary serializer but want in a certain format for better 
    /// human readability and occasional hand-editing.
    /// </remarks>
    public class DatabaseChangeRepository
    {
        public DatabaseChangeRepository()
        {
            this.KnownFileTypes = new KnownFileTypes();
        }

        private KnownFileTypes KnownFileTypes { get; set; }

        public void Save(DatabaseChanges changes, string filename)
        {
            changes.LastSavedAt = DateTime.Now;

            var xDoc = new XDocument();
            var xRoot = new XElement("DatabaseChanges");
            var xMeta = new XElement("Metadata");
            var xChanges = new XElement("Changes");
            var xIncluded = new XElement("Included");
            var xExcluded = new XElement("Excluded");

            xMeta.Add(new XElement("RootDatabaseFolder", changes.RootDatabaseFolder));
            xMeta.Add(new XElement("GeneratedAt", changes.GeneratedAt));
            xMeta.Add(new XElement("LastSavedAt", changes.LastSavedAt));
            xMeta.Add(new XElement("RootWorkItemId", changes.RootWorkItemId));
            xMeta.Add(new XElement("DeletedSubDirName", changes.DeletedSubDirName));

            var changeCount = 0;

            if (changes.IncludedChanges != null)
            {
                var included = changes.IncludedChanges.ToList();
                included.ForEach(c => AddChange(xIncluded, c));
                xIncluded.SetAttributeValue("Count", included.Count);
                changeCount += included.Count;
            }

            if (changes.ExcludedChanges != null)
            {
                var excluded = changes.ExcludedChanges.ToList();
                excluded.ForEach(c => AddChange(xExcluded, c));
                xExcluded.SetAttributeValue("Count", excluded.Count);
                changeCount += excluded.Count;
            }

            xChanges.SetAttributeValue("Count", changeCount);

            xChanges.Add(xIncluded, xExcluded);
            xRoot.Add(xMeta, xChanges);
            xDoc.Add(xRoot);
            xDoc.Save(filename);
        }

        public DatabaseChanges Load(string filename)
        {
            var changes = new DatabaseChanges();
            var xDoc = XDocument.Load(filename);

            var xMeta = xDoc.Root.Element("Metadata");
            changes.RootDatabaseFolder = xMeta.Element("RootDatabaseFolder").Value;
            changes.RootWorkItemId = Convert.ToInt32(xMeta.Element("RootWorkItemId").Value);
            changes.GeneratedAt = Convert.ToDateTime(xMeta.Element("GeneratedAt").Value);
            changes.LastSavedAt = Convert.ToDateTime(xMeta.Element("LastSavedAt").Value);
            changes.DeletedSubDirName = xMeta.Element("DeletedSubDirName").Value;

            var xChanges = xDoc.Root.Element("Changes");
            var xIncluded = xChanges.Element("Included");
            var xExcluded = xChanges.Element("Excluded");

            xIncluded.Elements().ToList().ForEach(x=> changes.IncludedChanges.Add(Change(x, changes)));
            xExcluded.Elements().ToList().ForEach(x => changes.ExcludedChanges.Add(Change(x, changes)));

            var index = 0;
            changes.IncludedChanges.ToList().ForEach(x=> x.Index = ++index);

            return changes;
        }

        private DatabaseChange Change(XElement xChange, DatabaseChanges changes)
        {
            var xLastChangeType = xChange.Element("ChangeTypes").Elements().LastOrDefault();
            var lastChangeType = ChangeTypes.None;
            if (null != xLastChangeType)
                lastChangeType = (ChangeTypes)Enum.Parse(typeof(ChangeTypes), xLastChangeType.Value);

            var filename = Path.Combine(changes.RootDatabaseFolder,  xChange.Element("FilePath").Value);
            var fileType = this.KnownFileTypes.GetTypeForFilenameExt(filename);
            var ext = fileType.GetFileExtensionForFile(filename);

            // Type is set via Extension
            // File is set via Filename
            var change = new DatabaseChange
            {
                Schema = xChange.Attribute("Schema").Value,
                Filename = filename,
                //FilePath = filename.Replace(changes.RootDatabaseFolder, string.Empty),
                FilePath = xChange.Element("FilePath").Value,
                Extension = ext,
                FirstChanged = DateTime.Parse(xChange.Element("FirstChanged").Value),
                LastChanged = DateTime.Parse(xChange.Element("LastChanged").Value),
                IsAttachment = Convert.ToBoolean(xChange.Element("IsAttachment").Value),
                IsManualAdd = Convert.ToBoolean(xChange.Element("IsManualAdd").Value)
            };

            var xServerItem = xChange.Element("ServerItem");

            if (null != xServerItem)
                change.ServerItem = xServerItem.Value;

            var xChangeTypes = xChange.Element("ChangeTypes");
            xChangeTypes.Elements().ToList().ForEach(x=> change.AddChangeType((ChangeTypes)Enum.Parse(typeof(ChangeTypes), x.Value)));

            var xTasks = xChange.Element("Tasks");
            xTasks.Elements().ToList().ForEach(xTask=>
                {
                    var taskInfo = new WorkItemInfo
                    {
                        AssignedTo = xTask.Attribute("AssignedTo").Value,
                        Id = Convert.ToInt32(xTask.Attribute("Id").Value),
                        State = xTask.Attribute("State").Value,
                        Title = xTask.Attribute("Title").Value
                    };
                    change.AddTask(taskInfo);
                });

            return change;
        }

        private void AddChange(XElement xParent, DatabaseChange change)
        {
            var xChange = new XElement("Change", 
                new XAttribute("File", change.File), 
                new XAttribute("Schema", change.Schema ?? string.Empty), 
                new XAttribute("Type", change.Type));

            xChange.Add(new XElement("FilePath", change.FilePath));
            xChange.Add(new XElement("FirstChanged", change.FirstChanged));
            xChange.Add(new XElement("IsAttachment", change.IsAttachment));
            xChange.Add(new XElement("IsManualAdd", change.IsManualAdd));
            xChange.Add(new XElement("LastChanged", change.LastChanged));
            xChange.Add(new XElement("ServerItem", change.ServerItem));

            var xChangeTypes = new XElement("ChangeTypes");
            change.ChangeTypes.ToList().ForEach(x=> xChangeTypes.Add(new XElement("ChangeType", x.ToString())));
            xChange.Add(xChangeTypes);

            var xTasks = new XElement("Tasks", new XAttribute("Count", change.Tasks.Count));

            change.Tasks.ToList().ForEach(x=>
                {
                    var xTask = new XElement(
                        "Task",
                        new XAttribute("Id", x.Id),
                        new XAttribute("AssignedTo", x.AssignedTo),
                        new XAttribute("State", x.State),
                        new XAttribute("Title", x.Title));
                    xTasks.Add(xTask);
                });

            xChange.Add(xTasks);
            xParent.Add(xChange);
        }
    }
}
