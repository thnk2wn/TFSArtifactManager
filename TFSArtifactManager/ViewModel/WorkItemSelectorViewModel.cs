namespace TFSArtifactManager.ViewModel
{
    public class WorkItemSelectorViewModel : AppViewModelBase
    {
        private int? _workItemId;

        public int? WorkItemId
        {
            get { return _workItemId; }
            set
            {
                if (_workItemId != value)
                {
                    _workItemId = value;
                    RaisePropertyChanged(() => WorkItemId);
                }
            }
        }
    }
}
