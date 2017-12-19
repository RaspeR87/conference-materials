using BetterTrainer.FaceRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrainer.PBI
{
    public class FacesInTime
    {
        public DateTime TimeStamp { get; set; }
        public LiveCameraResult CameraResult { get; set; }
    }
}
