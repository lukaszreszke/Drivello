namespace Drivello.Services;

public interface IActiveRentalsCounter
{
    void Increment();
    void Decrement();
}