using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarotraumaSaveFileBackup.App.Serialization
{
    public interface IBackupSerializer
    {
        /// <summary>
        /// Serializes the barotrauma save file and optional character data.
        /// </summary>
        /// <param name="saveFile">The full path to the barotrauma save file.</param>
        /// <param name="characterData">The full path to the barotrauma save file character data.</param>
        /// <param name="cancellationToken">Token to monitor for operation cancellation.</param>
        /// <returns>A task object representing the operation.</returns>
        Task SerializeAsync(string saveFile, string? characterData = null, CancellationToken cancellationToken = default);
    }
}
