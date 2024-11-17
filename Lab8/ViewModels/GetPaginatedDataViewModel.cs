using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class GetPaginatedDataViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public GetPaginatedDataViewModel()
        {
            _dataService = new DataService();
            GetPaginatedDataCommand = new RelayCommand(ExecuteGetPaginatedData);
        }

        public ICommand GetPaginatedDataCommand { get; }

        private string _nameFilter;
        public string NameFilter
        {
            get => _nameFilter;
            set => SetProperty(ref _nameFilter, value);
        }

        private string _addressFilter;
        public string AddressFilter
        {
            get => _addressFilter;
            set => SetProperty(ref _addressFilter, value);
        }

        private int _page;
        public int Page
        {
            get => _page;
            set => SetProperty(ref _page, value);
        }

        private int _pageSize;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private DataTable _data;
        public DataTable Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        private void ExecuteGetPaginatedData()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                Data = _dataService.GetPaginatedData(NameFilter, AddressFilter, Page, PageSize);
                ResultMessage = "Data retrieved successfully!";
                _dataService.CommitTransaction();
            }
            catch (Exception ex)
            {
                _dataService.RollbackTransaction();
                ResultMessage = $"Error: {ex.Message}";
            }
        }
    }
}
