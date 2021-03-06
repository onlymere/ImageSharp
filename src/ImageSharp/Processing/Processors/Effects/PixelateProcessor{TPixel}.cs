// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Common;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing.Processors.Effects
{
    /// <summary>
    /// Applies a pixelation effect processing to the image.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class PixelateProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        private readonly PixelateProcessor definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelateProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="definition">The <see cref="PixelateProcessor"/>.</param>
        /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
        /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
        public PixelateProcessor(Configuration configuration, PixelateProcessor definition, Image<TPixel> source, Rectangle sourceRectangle)
            : base(configuration, source, sourceRectangle)
        {
            this.definition = definition;
        }

        private int Size => this.definition.Size;

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source)
        {
            if (this.Size <= 0 || this.Size > source.Height || this.Size > source.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(this.Size));
            }

            int startY = this.SourceRectangle.Y;
            int endY = this.SourceRectangle.Bottom;
            int startX = this.SourceRectangle.X;
            int endX = this.SourceRectangle.Right;
            int size = this.Size;
            int offset = this.Size / 2;

            // Align start/end positions.
            int minX = Math.Max(0, startX);
            int maxX = Math.Min(source.Width, endX);
            int minY = Math.Max(0, startY);
            int maxY = Math.Min(source.Height, endY);

            // Reset offset if necessary.
            if (minX > 0)
            {
                startX = 0;
            }

            if (minY > 0)
            {
                startY = 0;
            }

            // Get the range on the y-plane to choose from.
            IEnumerable<int> range = EnumerableExtensions.SteppedRange(minY, i => i < maxY, size);

            Parallel.ForEach(
                range,
                this.Configuration.GetParallelOptions(),
                y =>
                    {
                        int offsetY = y - startY;
                        int offsetPy = offset;

                        // Make sure that the offset is within the boundary of the image.
                        while (offsetY + offsetPy >= maxY)
                        {
                            offsetPy--;
                        }

                        Span<TPixel> row = source.GetPixelRowSpan(offsetY + offsetPy);

                        for (int x = minX; x < maxX; x += size)
                        {
                            int offsetX = x - startX;
                            int offsetPx = offset;

                            while (x + offsetPx >= maxX)
                            {
                                offsetPx--;
                            }

                            // Get the pixel color in the centre of the soon to be pixelated area.
                            TPixel pixel = row[offsetX + offsetPx];

                            // For each pixel in the pixelate size, set it to the centre color.
                            for (int l = offsetY; l < offsetY + size && l < maxY; l++)
                            {
                                for (int k = offsetX; k < offsetX + size && k < maxX; k++)
                                {
                                    source[k, l] = pixel;
                                }
                            }
                        }
                    });
        }
    }
}
