using Arch.Core;
using System.Threading.Tasks;

public interface ICommand
{
    // All commands will have an Execute method.
    Task ExecuteAsync(TelnetSession session, World world, string[] args);
}