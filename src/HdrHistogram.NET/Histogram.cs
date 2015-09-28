﻿/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using System;
using HdrHistogram.NET.Utilities;
using LongBuffer = HdrHistogram.NET.Utilities.WrappedBuffer<long>;

namespace HdrHistogram.NET
{
    /**
     * <h3>A High Dynamic Range (HDR) Histogram</h3>
     * <p>
     * Histogram supports the recording and analyzing sampled data value counts across a configurable integer value
     * range with configurable value precision within the range. Value precision is expressed as the number of significant
     * digits in the value recording, and provides control over value quantization behavior across the value range and the
     * subsequent value resolution at any given level.
     * <p>
     * For example, a Histogram could be configured to track the counts of observed integer values between 0 and
     * 3,600,000,000 while maintaining a value precision of 3 significant digits across that range. Value quantization
     * within the range will thus be no larger than 1/1,000th (or 0.1%) of any value. This example Histogram could
     * be used to track and analyze the counts of observed response times ranging between 1 microsecond and 1 hour
     * in magnitude, while maintaining a value resolution of 1 microsecond up to 1 millisecond, a resolution of
     * 1 millisecond (or better) up to one second, and a resolution of 1 second (or better) up to 1,000 seconds. At it's
     * maximum tracked value (1 hour), it would still maintain a resolution of 3.6 seconds (or better).
     * <p>
     * Histogram tracks value counts in <b><code>long</code></b> fields. Smaller field types are available in the
     * {@link org.HdrHistogram.IntHistogram} and {@link org.HdrHistogram.ShortHistogram} implementations of
     * {@link org.HdrHistogram.AbstractHistogram}.
     * <p>
     * See package description for {@link org.HdrHistogram} for details.
     */
    public class Histogram : AbstractHistogram
    {
        long totalCount;
        readonly long[] counts;

        protected override int WordSizeInBytes => 8;

        protected override long GetCountAtIndex(int index) 
        {
            return counts[index];
        }

        protected override void IncrementCountAtIndex(int index) 
        {
            counts[index]++;
        }

        protected override void AddToCountAtIndex(int index, long value) 
        {
            counts[index] += value;
        }

        protected override void ClearCounts()
        {
            Array.Clear(counts, 0, counts.Length);
            totalCount = 0;
        }

        public override /*Histogram*/ AbstractHistogram Copy() 
        {
            Histogram copy = new Histogram(LowestTrackableValue, HighestTrackableValue, NumberOfSignificantValueDigits);
            copy.Add(this);
            return copy;
        }

        public override /*Histogram*/ AbstractHistogram CopyCorrectedForCoordinatedOmission(long expectedIntervalBetweenValueSamples) 
        {
            Histogram toHistogram = new Histogram(LowestTrackableValue, HighestTrackableValue, NumberOfSignificantValueDigits);
            toHistogram.AddWhileCorrectingForCoordinatedOmission(this, expectedIntervalBetweenValueSamples);
            return toHistogram;
        }

        public override long GetTotalCount() 
        {
            return totalCount;
        }

        protected override void SetTotalCount(long totalCount) 
        {
            this.totalCount = totalCount;
        }

        protected override void IncrementTotalCount() 
        {
            totalCount++;
        }

        protected override void AddToTotalCount(long value) 
        {
            totalCount += value;
        }

        public override int GetEstimatedFootprintInBytes() 
        {
            return (512 + (8 * counts.Length));
        }

        /**
         * Construct a Histogram given the Highest value to be tracked and a number of significant decimal digits. The
         * histogram will be constructed to implicitly track (distinguish from 0) values as low as 1.
         *
         * @param highestTrackableValue          The highest value to be tracked by the histogram. Must be a positive
         *                                       integer that is {@literal >=} 2.
         * @param numberOfSignificantValueDigits The number of significant decimal digits to which the histogram will
         *                                       maintain value resolution and separation. Must be a non-negative
         *                                       integer between 0 and 5.
         */
        public Histogram(long highestTrackableValue, int numberOfSignificantValueDigits) 
            : this(1, highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        /**
         * Construct a Histogram given the Lowest and Highest values to be tracked and a number of significant
         * decimal digits. Providing a lowestTrackableValue is useful is situations where the units used
         * for the histogram's values are much smaller that the minimal accuracy required. E.g. when tracking
         * time values stated in nanosecond units, where the minimal accuracy required is a microsecond, the
         * proper value for lowestTrackableValue would be 1000.
         *
         * @param lowestTrackableValue           The lowest value that can be tracked (distinguished from 0) by the histogram.
         *                                       Must be a positive integer that is {@literal >=} 1. May be internally rounded down to nearest
         *                                       power of 2.
         * @param highestTrackableValue          The highest value to be tracked by the histogram. Must be a positive
         *                                       integer that is {@literal >=} (2 * lowestTrackableValue).
         * @param numberOfSignificantValueDigits The number of significant decimal digits to which the histogram will
         *                                       maintain value resolution and separation. Must be a non-negative
         *                                       integer between 0 and 5.
         */
        public Histogram(long lowestTrackableValue, long highestTrackableValue,
                         int numberOfSignificantValueDigits) 
            : base(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits)
        {
            counts = new long[CountsArrayLength];
        }

        /**
         * Construct a new histogram by decoding it from a ByteBuffer.
         * @param buffer The buffer to decode from
         * @param minBarForHighestTrackableValue Force highestTrackableValue to be set at least this high
         * @return The newly constructed histogram
         */
        public static Histogram decodeFromByteBuffer(ByteBuffer buffer,
                                                     long minBarForHighestTrackableValue) 
        {
            return (Histogram)AbstractHistogram.DecodeFromByteBuffer(buffer, typeof(Histogram), minBarForHighestTrackableValue);
        }

        /**
         * Construct a new histogram by decoding it from a compressed form in a ByteBuffer.
         * @param buffer The buffer to encode into
         * @param minBarForHighestTrackableValue Force highestTrackableValue to be set at least this high
         * @return The newly constructed histogram
         * @throws DataFormatException on error parsing/decompressing the buffer
         */
        public static Histogram decodeFromCompressedByteBuffer(ByteBuffer buffer,
                                                               long minBarForHighestTrackableValue) //throws DataFormatException 
        {
            return (Histogram)DecodeFromCompressedByteBuffer(buffer, typeof(Histogram), minBarForHighestTrackableValue);
        }

        //private void readObject(ObjectInputStream o) // throws IOException, ClassNotFoundException 
        //{
        //    o.defaultReadObject();
        //}

        protected override void FillCountsArrayFromBuffer(ByteBuffer buffer, int length)
        {
            lock (UpdateLock)
            {
                buffer.asLongBuffer().get(counts, 0, length);
            }
        }

        // We try to cache the LongBuffer used in output cases, as repeated
        // output form the same histogram using the same buffer is likely:
        private LongBuffer cachedDstLongBuffer = null;
        private ByteBuffer cachedDstByteBuffer = null;
        private int cachedDstByteBufferPosition = 0;

        protected override void FillBufferFromCountsArray(ByteBuffer buffer, int length)
        {
            lock (UpdateLock)
            {
                if ((cachedDstLongBuffer == null) ||
                    (buffer != cachedDstByteBuffer) ||
                    (buffer.position() != cachedDstByteBufferPosition))
                {
                    cachedDstByteBuffer = buffer;
                    cachedDstByteBufferPosition = buffer.position();
                    cachedDstLongBuffer = buffer.asLongBuffer();
                }
                cachedDstLongBuffer.rewind();
                cachedDstLongBuffer.put(counts, 0, length);
            }
        }
    }
}
