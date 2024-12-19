using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.Security.Identity.Domain;

using ReactiveUI;

using TodoDomain.Entities;

using static Missionware.SharedLib.Serialization.SerializedPropertyBinder;

namespace TodoAppMinimal;

public partial class HomeView : UserControl
{
    IClient _client;
    public HomeView()
    {
        InitializeComponent();
    }

    public void SetupView(IClient client, DataItemCollection<ToDoItem> items)
    {
        TodoListBox.ItemsSource = items;
        _client = client;
        _client.DataItemPropertyChanged += _client_DataItemPropertyChanged;
    }

    private async void _client_DataItemPropertyChanged(object? sender, DataItemPropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToDoItem.IsChecked) && e.DataItem.IsModified)
        {
            var saveResult = await _client.SaveAsync(e.DataItem);

            // if not successful close form
            if (!saveResult.WasSuccessful)
                _client.ResetAllMonitoredItems();
        }
    }    

    // Handle adding new Todo items
    private async void NewTodoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(NewTodoTextBox.Text))
        {
            var item = new ToDoItem { Description = NewTodoTextBox.Text };

            // save
            var saveResult = await _client.SaveAsync(item);

            // if not successful close form
            if (!saveResult.WasSuccessful)
                _client.ResetAllMonitoredItems();
            else
                NewTodoTextBox.Text = string.Empty;
        }
    }

    private async void ItemTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && e.Key == Key.Enter)
        {
            var item = textBox.DataContext as ToDoItem;

            if (item != null && item.IsModified)
            {
                // save
                var saveResult = await _client.SaveAsync(item);

                // if not successful close form
                if (!saveResult.WasSuccessful)
                    _client.ResetAllMonitoredItems();
                else
                    NewTodoTextBox.Focus();
            }
        }
    }

    // Apply changes when the TextBox loses focus
    private void ItemTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Exit edit mode
            textBox.IsReadOnly = true;
            textBox.Background = Brushes.Transparent; // Revert background

            _client.ResetAllMonitoredItems();
        }
    }

    private void ItemTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Exit edit mode
            textBox.CaretBrush = Brushes.Transparent;
            textBox.BorderBrush = Brushes.Transparent;
        }
    }

    private void ItemTextBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Enter edit mode
            textBox.IsReadOnly = false;
            textBox.CaretBrush = Brushes.White;
            textBox.CaretIndex = textBox.Text.Length; // Move the cursor to the end
            textBox.SelectionStart = textBox.CaretIndex;
            textBox.SelectionEnd = textBox.CaretIndex;
        }
    }

    private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var item = button.DataContext as ToDoItem;

            // check the item is not null
            if (item != null)
            {
                // mark for deletion
                item.MarkForDeletion();

                // save
                ClientTransactionInfo saveResult = await _client.SaveAsync(item);

                // if not success unmark (reset edit)
                if (!saveResult.WasSuccessful)
                    item.UnMarkForDeletion();

            }
        }
    }
}

