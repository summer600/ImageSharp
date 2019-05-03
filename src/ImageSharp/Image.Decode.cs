﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Linq;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Memory;

namespace SixLabors.ImageSharp
{
    /// <content>
    /// Adds static methods allowing the decoding of new images.
    /// </content>
    public abstract partial class Image
    {
        /// <summary>
        /// Creates an <see cref="Image{TPixel}"/> instance backed by an uninitialized memory buffer.
        /// This is an optimized creation method intended to be used by decoders.
        /// The image might be filled with memory garbage.
        /// </summary>
        /// <typeparam name="TPixel">The pixel type</typeparam>
        /// <param name="configuration">The <see cref="Configuration"/></param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="metadata">The <see cref="ImageMetadata"/></param>
        /// <returns>The result <see cref="Image{TPixel}"/></returns>
        internal static Image<TPixel> CreateUninitialized<TPixel>(
            Configuration configuration,
            int width,
            int height,
            ImageMetadata metadata)
            where TPixel : struct, IPixel<TPixel>
        {
            Buffer2D<TPixel> uninitializedMemoryBuffer =
                configuration.MemoryAllocator.Allocate2D<TPixel>(width, height);
            return new Image<TPixel>(configuration, uninitializedMemoryBuffer.MemorySource, width, height, metadata);
        }

        /// <summary>
        /// By reading the header on the provided stream this calculates the images format.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The mime type or null if none found.</returns>
        private static IImageFormat InternalDetectFormat(Stream stream, Configuration config)
        {
            // This is probably a candidate for making into a public API in the future!
            int maxHeaderSize = config.MaxHeaderSize;
            if (maxHeaderSize <= 0)
            {
                return null;
            }

            using (IManagedByteBuffer buffer = config.MemoryAllocator.AllocateManagedByteBuffer(maxHeaderSize, AllocationOptions.Clean))
            {
                long startPosition = stream.Position;
                stream.Read(buffer.Array, 0, maxHeaderSize);
                stream.Position = startPosition;
                return config.ImageFormatsManager.FormatDetectors.Select(x => x.DetectFormat(buffer.GetSpan())).LastOrDefault(x => x != null);
            }
        }

        /// <summary>
        /// By reading the header on the provided stream this calculates the images format.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="format">The IImageFormat.</param>
        /// <returns>The image format or null if none found.</returns>
        private static IImageDecoder DiscoverDecoder(Stream stream, Configuration config, out IImageFormat format)
        {
            format = InternalDetectFormat(stream, config);

            return format != null
                ? config.ImageFormatsManager.FindDecoder(format)
                : null;
        }

#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
        /// <summary>
        /// Decodes the image stream to the current image.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="config">the configuration.</param>
        /// <typeparam name="TPixel">The pixel format.</typeparam>
        /// <returns>
        /// A new <see cref="Image{TPixel}"/>.
        /// </returns>
        private static (Image<TPixel> img, IImageFormat format) Decode<TPixel>(Stream stream, Configuration config)
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly
            where TPixel : struct, IPixel<TPixel>
        {
            IImageDecoder decoder = DiscoverDecoder(stream, config, out IImageFormat format);
            if (decoder is null)
            {
                return (null, null);
            }

            Image<TPixel> img = decoder.Decode<TPixel>(config, stream);
            return (img, format);
        }
        
        private static (Image img, IImageFormat format) Decode(Stream stream, Configuration config)
        {
            IImageDecoder decoder = DiscoverDecoder(stream, config, out IImageFormat format);
            if (decoder is null)
            {
                return (null, null);
            }

            Image img = decoder.Decode(config, stream);
            return (img, format);
        }

        /// <summary>
        /// Reads the raw image information from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="config">the configuration.</param>
        /// <returns>
        /// The <see cref="IImageInfo"/> or null if suitable info detector not found.
        /// </returns>
        private static IImageInfo InternalIdentity(Stream stream, Configuration config)
        {
            var detector = DiscoverDecoder(stream, config, out IImageFormat _) as IImageInfoDetector;
            return detector?.Identify(config, stream);
        }
    }
}