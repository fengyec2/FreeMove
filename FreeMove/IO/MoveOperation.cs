// FreeMove -- Move directories without breaking shortcuts or installations 
//    Copyright(C) 2020  Luca De Martini

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMove.IO
{
    class MoveOperation : IOOperation
    {
        string pathFrom;
        string pathTo;
        bool sameDrive;
        bool createDestination;
        CancellationTokenSource cts = new CancellationTokenSource();
        CopyOperation innerCopy;

        /// <summary>
        /// 创建移动操作，并按当前界面状态决定是否预先创建目标目录。
        /// </summary>
        public MoveOperation(string pathFrom, string pathTo, bool createDestination)
        {
            this.pathFrom = pathFrom;
            this.pathTo = pathTo;
            this.createDestination = createDestination;
            sameDrive = string.Equals(Path.GetPathRoot(pathFrom), Path.GetPathRoot(pathTo), StringComparison.OrdinalIgnoreCase);
            if (this.createDestination && !Directory.Exists(pathTo))
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetParent(pathTo).FullName);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    if (e is UnauthorizedAccessException)
                    {
                        throw new UnauthorizedAccessException(Properties.Resources.ResourceManager.GetString("MoveOp_CreateDestUnauthorized"));
                    }
                    else
                    {
                        throw new IOException(Properties.Resources.ResourceManager.GetString("MoveOp_CreateDestIOError"));
                    }
                }
            }
            innerCopy = new CopyOperation(pathFrom, pathTo);
        }

        public override void Cancel()
        {
            cts.Cancel();
            innerCopy?.Cancel();
        }

        public override async Task Run()
        {
            innerCopy.ProgressChanged += (sender, e) => OnProgressChanged(e);
            innerCopy.Start += (sender, e) => OnStart(e);
            try
            {
                if (sameDrive)
                {
                    try
                    {
                        await Task.Run(() => Directory.Move(pathFrom, pathTo), cts.Token);
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        throw new MoveFailedException(Properties.Resources.ResourceManager.GetString("MoveOp_MoveSameDriveFailed"), e);
                    }
                }
                else
                {
                    try
                    {
                        await innerCopy.Run();
                    }
                    catch (Exception e) when (!(e is OperationCanceledException)) // Wrap inner exceptions to signal which phase failed
                    {
                        throw new CopyFailedException(Properties.Resources.ResourceManager.GetString("MoveOp_CopyFailed"), e);
                    }

                    cts.Token.ThrowIfCancellationRequested();
                    try
                    {
                        Directory.Delete(pathFrom, true);
                    }
                    catch (Exception e)
                    {
                        throw new DeleteFailedException(Properties.Resources.ResourceManager.GetString("MoveOp_DeleteOldFailed"), e);
                    }
                }
            }
            finally
            {
                OnEnd(new EventArgs());
            }
        }
        public class MoveFailedException : Exception
        {
            public MoveFailedException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class CopyFailedException : Exception
        {
            public CopyFailedException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class DeleteFailedException : Exception
        {
            public DeleteFailedException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
