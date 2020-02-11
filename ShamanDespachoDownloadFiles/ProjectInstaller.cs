using System.ComponentModel;
using System.Configuration.Install;

namespace ShamanDespachoDownloadFilesNoShaman
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
