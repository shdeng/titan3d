﻿/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2013-2021  Denis Kuzmin <x-3F@outlook.com> github/3F
 * Copyright (c) MvsSln contributors https://github.com/3F/MvsSln/graphs/contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace net.r_eg.MvsSln.Core.SlnHandlers
{
    using TransactSection = TransactTracking<ISection, IList<ISection>>;

    public sealed class Svc: ISvc
    {
        private StreamReader stream;

        private TransactTracking<ISection, IList<ISection>> tracking;

        private long nline = 0;

        private object sync = new object();

        /// <summary>
        /// Used encoding for all data.
        /// </summary>
        public Encoding CurrentEncoding
        {
            get => stream?.CurrentEncoding;
        }

        /// <summary>
        /// Prepared solution data.
        /// </summary>
        public ISlnResultSvc Sln
        {
            get;
            set;
        }

        /// <summary>
        /// Unspecified storage of the user scope.
        /// </summary>
        public Dictionary<Guid, object> UData
        {
            get;
            set;
        } = new Dictionary<Guid, object>();

        /// <summary>
        /// Reads a line of characters from stream.
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            lock(sync)
            {
                ++nline;
                return stream?.ReadLine();
            }
        }

        /// <summary>
        /// Reads a line of characters from stream with tracking.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public string ReadLine(object handler)
        {
            string line = ReadLine();
            Track(line, handler);
            return line;
        }

        /// <summary>
        /// Resets stream and its related data.
        /// </summary>
        public void ResetStream()
        {
            if(stream != null) {
                nline = stream.BaseStream.Seek(0, SeekOrigin.Begin);
                return;
            }
            nline = 0;
        }

        /// <summary>
        /// Non-Transact tracking for line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="handler">Specific handler if used, or null as an unspecified.</param>
        public ISection Track(RawText line, object handler = null)
        {
            if((Sln.ResultType & SlnItems.Map) != SlnItems.Map) {
                return null;
            }

            lock(sync)
            {
                tracking?.Commit(); // to commit all delayed sections

                ISection section = new Section(handler, line, nline);
                Sln.Map.Add(section);
                return section;
            }
        }

        /// <summary>
        /// Transact tracking for line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="handler">Specific handler if used, or null as an unspecified.</param>
        /// <returns></returns>
        public TransactSection TransactTrack(RawText line, object handler = null)
        {
            return TransactTrack(out ISection ss, line, handler);
        }

        /// <summary>
        /// Transact tracking for line.
        /// </summary>
        /// <param name="section">Provides requested section.</param>
        /// <param name="line"></param>
        /// <param name="handler">Specific handler if used, or null as an unspecified.</param>
        /// <returns></returns>
        public TransactSection TransactTrack(out ISection section, RawText line, object handler = null)
        {
            section = null;

            if(tracking == null 
                || (Sln.ResultType & SlnItems.Map) != SlnItems.Map)
            {
                return null;
            }

            section = new Section(handler, line, nline);
            return tracking.Track(section);
        }

        /// <param name="reader"></param>
        /// <param name="rsln"></param>
        public Svc(StreamReader reader, ISlnResultSvc rsln)
            : this(reader)
        {
            Sln         = rsln ?? throw new ArgumentNullException();
            tracking    = new TransactTracking<ISection, IList<ISection>>(Sln.Map);
        }

        /// <param name="reader"></param>
        public Svc(StreamReader reader)
        {
            stream = reader ?? throw new ArgumentNullException();
        }
    }
}
