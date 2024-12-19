using Avalonia.Controls;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.SharedLib.Avalonia;

using TodoApp.ViewModels;

using TodoDomain.Entities;

namespace TodoApp.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    public void Setup(IClient client, DataItemCollection<ToDoItem> items)
    {
        var vm = new HomeViewModel();
        vm.ListItems = items;
        DataContext = vm;
    }

}
