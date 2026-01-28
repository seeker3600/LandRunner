using LandRunner.ViewModels;

namespace LandRunnerTest;

/// <summary>
/// RelayCommand のテスト
/// コマンド実行と CanExecute ロジックを検証
/// </summary>
public class RelayCommandTests
{
    [Fact]
    public void RelayCommand_Execute_InvokesAction()
    {
        // Arrange
        bool executed = false;
        var command = new RelayCommand(() => executed = true);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void RelayCommand_CanExecute_ReturnsTrue_WhenNoCondition()
    {
        // Arrange
        var command = new RelayCommand(() => { });

        // Act
        var canExecute = command.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void RelayCommand_CanExecute_RespectsPredicate()
    {
        // Arrange
        bool isEnabled = false;
        var command = new RelayCommand(() => { }, () => isEnabled);

        // Act & Assert
        Assert.False(command.CanExecute(null));

        isEnabled = true;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task AsyncRelayCommand_Execute_InvokesAsyncAction()
    {
        // Arrange
        bool executed = false;
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Act
        command.Execute(null);
        await Task.Delay(50);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void AsyncRelayCommand_CanExecute_ReturnsTrue_WhenNotExecuting()
    {
        // Arrange
        var command = new AsyncRelayCommand(async () => await Task.Delay(10));

        // Act
        var canExecute = command.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }
}
