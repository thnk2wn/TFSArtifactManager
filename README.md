# TFSArtifactManager

TFS Artifact Manager is a tool intended for downloading application "artifacts", such as database scripts and report files, from TFS. It pulls down these files from task source control changesets and attachments, organizes them, outputs task and change information, and helps combine and package database object changes. The tool's primary value is doing this in mass for a work item such as a Project work item, where it will enumerate all linked scenarios, issues, bugs, and tasks, providing the complete set of non-.net source code changes that need to be deployed for a given software release. 

For detailed information on this project please visit http://www.geoffhudik.com/tech/2011/8/17/tfs-artifact-manager.html
