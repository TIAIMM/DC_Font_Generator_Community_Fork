using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DC_Font_Generator.WinUI.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> execute;
    private readonly Func<bool> canExecute;
    private bool isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return !isRunning && (canExecute == null || canExecute());
    }

    public async void Execute(object parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            isRunning = true;
            RaiseCanExecuteChanged();
            await execute().ConfigureAwait(true);
        }
        finally
        {
            isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
