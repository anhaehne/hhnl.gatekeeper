using System;
using System.Collections.Generic;
using System.Text;

namespace hhnl.gatekeeper.Models
{
    public class FacePrediction
    {
        public int RectLeft { get; set; }

        public int RectTop { get; set; }

        public int RectRight { get; set; }

        public int RectBottom { get; set; }

        public long ClusterId { get; set; }

        public decimal Confidence { get; set; }
    }
}
