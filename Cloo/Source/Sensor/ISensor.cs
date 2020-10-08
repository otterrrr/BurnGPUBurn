/*

Copyright (c) 2020 Taesik Yoon

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Cloo.Sensor
{
    /// <summary> Common SensorType </summary>
    public enum SensorType
    {
        /// <summary> Workload ratio from 0 to 100 (%) </summary>
        GFX_LOAD = 0,
        /// <summary> GPU Core Temperature </summary>
        GFX_TEMPERATURE = 1,
        /// <summary> GPU Core Power Consumption (W) </summary>
        GFX_POWER = 2,
    }

    /// <summary> Common interface to Sensor object </summary>
    public interface ISensor
    {
        /// <summary> GPU Core Load </summary>
        float? Load { get; }
        /// <summary> GPU Core Temperature </summary>
        float? Temperature { get; }
        /// <summary> GPU Core Power Consumption </summary>
        float? Power { get; }
    }
}
