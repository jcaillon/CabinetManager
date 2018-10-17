#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ArchiverProgressionEventArgs.cs) is part of Oetools.Utilities.
// 
// Oetools.Utilities is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Utilities is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Utilities. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;

namespace CabinetManager.@internal {
    
    internal class CabProgressionEventArgs : EventArgs, ICabProgressionEventArgs {
        
        /// <summary>
        /// The type of event.
        /// </summary>
        public CabEventType EventType { get; private set; }

        /// <summary>
        /// The path of the cabinet file concerned by this event
        /// </summary>
        public string CabPath { get; private set; }

        /// <summary>
        /// The relative path, within the cabinet file, concerned by this event.
        /// </summary>
        public string RelativePathInCab { get; private set; }
        
        /// <summary>
        /// The total percentage already done for the current process, from 0 to 100.
        /// </summary>
        public double PercentageDone { get; private set; }
        
        internal static CabProgressionEventArgs NewCompletedFile(string cabPath, string relativePathInCab) {
            return new CabProgressionEventArgs {
                CabPath = cabPath,
                EventType = CabEventType.FileProcessed,
                RelativePathInCab = relativePathInCab
            };
        }
        
        internal static CabProgressionEventArgs NewCompletedCabinet(string cabPath) {
            return new CabProgressionEventArgs {
                CabPath = cabPath,
                EventType = CabEventType.CabinetCompleted
            };
        }
        
        internal static CabProgressionEventArgs NewProgress(string cabPath, string currentRelativePathInCab, double percentageDone) {
            return new CabProgressionEventArgs {
                CabPath = cabPath,
                EventType = CabEventType.GlobalProgression,
                PercentageDone = percentageDone,
                RelativePathInCab = currentRelativePathInCab
            };
        }

    }
}