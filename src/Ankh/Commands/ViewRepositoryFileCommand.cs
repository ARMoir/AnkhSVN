// $Id$
using EnvDTE;

using System.IO;
using System.Collections;

using Ankh.RepositoryExplorer;
using AnkhSvn.Ids;

namespace Ankh.Commands
{
    /// <summary>
    /// A command that lets you view a repository file.
    /// </summary>
    [VSNetCommand(AnkhCommand.ViewRepositoryFile,
		"ViewRepositoryFile", Tooltip="View this file", Text = "In VS.NET" ),
    VSNetControl( "ReposExplorer.View", Position = 1 ) ]
    public abstract class ViewRepositoryFileCommand : CommandBase
    {
        #region ICommand Members
        public override EnvDTE.vsCommandStatus QueryStatus(IContext context)
        {
            // we enable it if it's a file.
            return context.RepositoryExplorer.SelectedNode != null &&
                !context.RepositoryExplorer.SelectedNode.IsDirectory ? 
                Enabled : Disabled;
        }
        #endregion
      
    }
}



