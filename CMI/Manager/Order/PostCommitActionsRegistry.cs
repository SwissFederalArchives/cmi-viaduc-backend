using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Erlaubt es, bestimmte Aktionen erst nach dem Commit aller in dieser Transaktion verarbeiteten
    ///     Operationen durchzuführen.
    /// </summary>
    public class PostCommitActionsRegistry : IRunAll
    {
        private readonly List<Action> actions = new List<Action>();

        async Task IRunAll.RunAll()
        {
            foreach (var action in actions)
            {
                try
                {
                    await Task.Run(action);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Bei den PostCommitActions ist ein Fehler aufgetreten");
                    throw;
                }
            }
        }

        public void RegisterPostCommitAction(Action a)
        {
            actions.Add(a);
        }
    }

    public interface IRunAll
    {
        Task RunAll();
    }
}