using System;
using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TFSWorkItemChangesetInfo.IO
{
    public static class ChangeSetFileTagger
    {
        public static void Tag(Changeset changeset, Change change, string fileName)
        {
            var sr = new StreamReader(fileName);
            var tempFile = sr.ReadToEnd();
            sr.Close();
            var sw = new StreamWriter(fileName);
            sw.WriteLine("----------------------------------------------------");
            sw.WriteLine("-- ChangesetId: {0}{1}-- ServerItem: {2}", changeset.ChangesetId, Environment.NewLine, change.Item.ServerItem);

            sw.WriteLine("-- Changeset Creation Date: {0}, Owner: {1}, Committer: {2}", changeset.CreationDate, changeset.Owner, changeset.Committer);

            foreach (var workItem in changeset.WorkItems)
            {
                sw.WriteLine("-- WorkItem: " + workItem.Id);
            }

            sw.WriteLine("----------------------------------------------------" + Environment.NewLine);
            sw.Write(tempFile);
            sw.Close();
        }
    }
}
