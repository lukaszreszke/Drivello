using Drivello.Infrastructure;

namespace Drivello.Services;

public class ActiveRentalsCounter : IActiveRentalsCounter
{
    private static readonly Lazy<GlobalStateManager> _globalState = new(() => new GlobalStateManager());
    public static GlobalStateManager GlobalState => _globalState.Value;
    public void Increment()
    {
        GlobalState.TotalActiveRentals++;
    }

    public void Decrement()
    {
        GlobalState.TotalActiveRentals--;
    }
}