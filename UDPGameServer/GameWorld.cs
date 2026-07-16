using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGameServer
{
    internal class GameWorld
    {
        float ballMoveSpeed=4;
        /// <summary>
        ///this is half window size from mono game. used to define the world in pixels...
        ///Should probably be in a different format and fitted the clients resolution
        /// </summary>
        float ballPosX = 400;
        float ballPosY = 240;
        int snapShotId = 0;
        public GameWorld()
        {
        }    
        public void UpdateBallMovement(MovementUpdate mov)
        {
            if (mov.Moveleft)
            {
                ballPosX -= 1 * ballMoveSpeed;
            }
            else
            {
                ballPosX += 1 * ballMoveSpeed;
            }
            snapShotId = mov.SequenceNumber;
        }
        public SnapShot GetWorldStateSnapShot()
        {
            return new SnapShot() {ballPosY=ballPosY, ballPosX=ballPosX,SnapSeqId =snapShotId };
        }
    }
}
