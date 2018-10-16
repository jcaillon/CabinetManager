#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (CfSaveProgression.cs) is part of CabinetManager.
// 
// CabinetManager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CabinetManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with CabinetManager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;

namespace CabinetManager.core {
    
    internal class CfSaveProgressionEventArgs : EventArgs {
        
        public CfSaveProgressionType ProgressionType { get; private set; }
        
        public string RelativePathInCab { get; private set; }
        
        public decimal FilePercentageDone { get; private set; }

        public static CfSaveProgressionEventArgs NewFinishedFile(string relativePathInCab) {
            return new CfSaveProgressionEventArgs {
                ProgressionType = CfSaveProgressionType.FileCompleted,
                RelativePathInCab = relativePathInCab,
                FilePercentageDone = 100
            };
        }

        public static CfSaveProgressionEventArgs NewFileProgress(string relativePathInCab, decimal filePercentageDone) {
            return new CfSaveProgressionEventArgs {
                ProgressionType = CfSaveProgressionType.FileProgression,
                RelativePathInCab = relativePathInCab,
                FilePercentageDone = filePercentageDone
            };
        }
    }
}