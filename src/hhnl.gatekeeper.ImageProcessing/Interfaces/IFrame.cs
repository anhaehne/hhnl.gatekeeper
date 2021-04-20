﻿using System;
using System.Drawing;
using System.Threading.Tasks;

namespace hhnl.gatekeeper.ImageProcessing.Interfaces
{
    public interface IFrame : IManagedObject
    {
        Bitmap Original { get; }
        
        Task<Bitmap> ToScaledBitmapAsync(int width, int height);
        
        int OriginalHeight { get; }
        
        int OriginalWidth { get; }
        
        long Id { get; }
    }
}