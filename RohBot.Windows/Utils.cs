using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RohBot
{
    public enum ImageScale
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Original = 3
    }

    internal static class Utils
    {
        public static ScrollViewer GetScrollViewer(DependencyObject element)
        {
            var viewer = element as ScrollViewer;
            if (viewer != null)
            {
                return viewer;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static int BinarySearch<T>(this IList<T> list, T value)
            where T : IComparable<T>
        {
            int lo = 0;
            int hi = list.Count - 1;

            while (lo <= hi)
            {
                int i = lo + ((hi - lo) / 2);
                int order = list[i].CompareTo(value);

                if (order == 0)
                    return i;

                if (order < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return ~lo;
        }

        public static Task<IRandomAccessStream> ProcessImage(
            IRandomAccessStream imageData, ImageScale scale)
        {
            uint maxDim = 0;
            switch (scale)
            {
                case ImageScale.Small:
                    maxDim = 500;
                    break;
                
                case ImageScale.Medium:
                    maxDim = 1000;
                    break;
                
                case ImageScale.Large:
                    maxDim = 2000;
                    break;
            }

            return ProcessImage(imageData, maxDim, maxDim);
        }

        public static Task<IRandomAccessStream> ProcessImage(
            IRandomAccessStream imageData, uint reqWidth, uint reqHeight)
        {
            return Task.Run(async () =>
                await CorrectRotation(await ResizeImage(imageData, reqWidth, reqHeight)));
        }

        private static async Task<IRandomAccessStream> CorrectRotation(IRandomAccessStream imageStream)
        {
            const string orientationKey = "System.Photo.Orientation";

            var decoder = await BitmapDecoder.CreateAsync(imageStream);

            try
            {
                var properties = await decoder.BitmapProperties.GetPropertiesAsync(new[] { orientationKey });
                if (!properties.TryGetValue(orientationKey, out var orientationValue) ||
                    (ushort)orientationValue.Value == 1)
                {
                    // normal orientation doesn't need to be processed
                    return imageStream;
                }
            }
            catch
            {
                // format doesn't support orientation
                return imageStream;
            }

            using (imageStream)
            {
                var bitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);
                
                var memStream = new InMemoryRandomAccessStream();

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memStream);
                encoder.SetSoftwareBitmap(bitmap);
                await encoder.FlushAsync();
                
                return memStream;
            }
        }

        private static async Task<IRandomAccessStream> ResizeImage(
           IRandomAccessStream imageStream, uint reqWidth, uint reqHeight)
        {
            if (reqWidth == 0 && reqHeight == 0)
                return imageStream;

            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            if (decoder.PixelHeight <= reqHeight && decoder.PixelWidth <= reqWidth)
                return imageStream;

            using (imageStream)
            {
                var resizedStream = new InMemoryRandomAccessStream();

                var encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                var widthRatio = (double)reqWidth / decoder.PixelWidth;
                var heightRatio = (double)reqHeight / decoder.PixelHeight;

                var scaleRatio = Math.Min(widthRatio, heightRatio);

                if (reqWidth == 0)
                    scaleRatio = heightRatio;

                if (reqHeight == 0)
                    scaleRatio = widthRatio;

                var aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                var aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;

                encoder.BitmapTransform.ScaledHeight = aspectHeight;
                encoder.BitmapTransform.ScaledWidth = aspectWidth;

                await encoder.FlushAsync();
                resizedStream.Seek(0);
                return resizedStream;
            }
        }

        public static string GetNamedStringOrNull(this JsonObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return null;

            var value = obj.GetNamedValue(key);
            if (value.ValueType != JsonValueType.String)
                return null;

            return value.GetString();
        }
    }
}
