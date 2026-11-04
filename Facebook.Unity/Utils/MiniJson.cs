/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Facebook.MiniJSON
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    // Example usage:
    //
    //  using UnityEngine;
    //  using System.Collections;
    //  using System.Collections.Generic;
    //  using MiniJSON;
    //
    //  public class MiniJSONTest : MonoBehaviour {
    //      void Start () {
    //          var jsonString = "{ \"array\": [1.44,2,3], " +
    //                          "\"object\": {\"key1\":\"value1\", \"key2\":256}, " +
    //                          "\"string\": \"The quick brown fox \\\"jumps\\\" over the lazy dog \", " +
    //                          "\"unicode\": \"\\u3041 Men\u00fa sesi\u00f3n\", " +
    //                          "\"int\": 65536, " +
    //                          "\"float\": 3.1415926, " +
    //                          "\"bool\": true, " +
    //                          "\"null\": null }";
    //
    //          var dict = Json.Deserialize(jsonString) as Dictionary<string,object>;
    //
    //          Debug.Log("deserialized: " + dict.GetType());
    //          Debug.Log("dict['array'][0]: " + ((List<object>) dict["array"])[0]);
    //          Debug.Log("dict['string']: " + (string) dict["string"]);
    //          Debug.Log("dict['float']: " + (double) dict["float"]); // floats come out as doubles
    //          Debug.Log("dict['int']: " + (long) dict["int"]); // ints come out as longs
    //          Debug.Log("dict['unicode']: " + (string) dict["unicode"]);
    //
    //          var str = Json.Serialize(dict);
    //
    //          Debug.Log("serialized: " + str);
    //      }
    //  }

    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
    /// All numbers are parsed to doubles.
    /// </summary>
    public static class Json
    {
        // interpret all numbers as if they are english US formatted numbers
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static NumberFormatInfo numberFormat = (new CultureInfo("en-US")).NumberFormat;

        /// <summary>
        /// Parses the string json into a value.
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false.</returns>
        public static object Deserialize(string json)
        {
            // save the string for debug information
            if (json == null)
            {
                return null;
            }

            return Parser.Parse(json);
        }

        /// <summary>
        /// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string.
        /// </summary>
        /// <param name="obj">A Dictionary&lt;string, object&gt; / List&lt;object&gt;.</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable.</returns>
        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private sealed class Parser : IDisposable
        {
            private const string WhiteSpace = " \t\n\r";
            private const string WordBreak = " \t\n\r{}[],:\"";

            private StringReader json;

            private Parser(string jsonString)
            {
                this.json = new StringReader(jsonString);
            }

            private enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            private char PeekChar
            {
                get
                {
                    return Convert.ToChar(this.json.Peek());
                }
            }

            private char NextChar
            {
                get
                {
                    return Convert.ToChar(this.json.Read());
                }
            }

            private string NextWord
            {
                get
                {
                    StringBuilder word = new StringBuilder();

                    while (WordBreak.IndexOf(this.PeekChar) == -1)
                    {
                        word.Append(this.NextChar);

                        if (this.json.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return word.ToString();
                }
            }

            private TOKEN NextToken
            {
                get
                {
                    this.EatWhitespace();

                    if (this.json.Peek() == -1)
                    {
                        return TOKEN.NONE;
                    }

                    char c = this.PeekChar;
                    switch (c)
                    {
                        case '{':
                            return TOKEN.CURLY_OPEN;
                        case '}':
                            this.json.Read();
                            return TOKEN.CURLY_CLOSE;
                        case '[':
                            return TOKEN.SQUARED_OPEN;
                        case ']':
                            this.json.Read();
                            return TOKEN.SQUARED_CLOSE;
                        case ',':
                            this.json.Read();
                            return TOKEN.COMMA;
                        case '"':
                            return TOKEN.STRING;
                        case ':':
                            return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return TOKEN.NUMBER;
                    }

                    string word = this.NextWord;

                    switch (word)
                    {
                        case "false":
                            return TOKEN.FALSE;
                        case "true":
                            return TOKEN.TRUE;
                        case "null":
                            return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                this.json.Dispose();
                this.json = null;
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();

                // ditch opening brace
                this.json.Read();

                // {
                while (true)
                {
                    switch (this.NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            // name
                            string name = this.ParseString();
                            if (name == null)
                            {
                                return null;
                            }

                            // :
                            if (this.NextToken != TOKEN.COLON)
                            {
                                return null;
                            }

                            // ditch the colon
                            this.json.Read();

                            // value
                            table[name] = this.ParseValue();
                            break;
                    }
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new List<object>();

                // ditch opening bracket
                this.json.Read();

                // [
                var parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = this.NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            object value = this.ParseByToken(nextToken);

                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                TOKEN nextToken = this.NextToken;
                return this.ParseByToken(nextToken);
            }

            private object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return this.ParseString();
                    case TOKEN.NUMBER:
                        return this.ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return this.ParseObject();
                    case TOKEN.SQUARED_OPEN:
                        return this.ParseArray();
                    case TOKEN.TRUE:
                        return true;
                    case TOKEN.FALSE:
                        return false;
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                StringBuilder s = new StringBuilder();
                char c;

                // ditch opening quote
                this.json.Read();

                bool parsing = true;
                while (parsing)
                {
                    if (this.json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    c = this.NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (this.json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = this.NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    s.Append(c);
                                    break;
                                case 'b':
                                    s.Append('\b');
                                    break;
                                case 'f':
                                    s.Append('\f');
                                    break;
                                case 'n':
                                    s.Append('\n');
                                    break;
                                case 'r':
                                    s.Append('\r');
                                    break;
                                case 't':
                                    s.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new StringBuilder();

                                    for (int i = 0; i < 4; i++)
                                    {
                                        hex.Append(this.NextChar);
                                    }

                                    s.Append((char)Convert.ToInt32(hex.ToString(), 16));
                                    break;
                            }

                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            private object ParseNumber()
            {
                string number = this.NextWord;

                if (number.IndexOf('.') == -1)
                {
                    return long.Parse(number, numberFormat);
                }

                return double.Parse(number, numberFormat);
            }

            private void EatWhitespace()
            {
                while (WhiteSpace.IndexOf(this.PeekChar) != -1)
                {
                    this.json.Read();

                    if (this.json.Peek() == -1)
                    {
                        break;
                    }
                }
            }
        }

        private sealed class Serializer
        {
            private StringBuilder builder;

            private Serializer()
            {
                this.builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();

                instance.SerializeValue(obj);

                return instance.builder.ToString();
            }

            private void SerializeValue(object value)
            {
                IList asList;
                IDictionary asDict;
                string asStr;

                if (value == null)
                {
                    this.builder.Append("null");
                }
                else if ((asStr = value as string) != null)
                {
                    this.SerializeString(asStr);
                }
                else if (value is bool)
                {
                    this.builder.Append(value.ToString().ToLower());
                }
                else if ((asList = value as IList) != null)
                {
                    this.SerializeArray(asList);
                }
                else if ((asDict = value as IDictionary) != null)
                {
                    this.SerializeObject(asDict);
                }
                else if (value is char)
                {
                    this.SerializeString(value.ToString());
                }
                else
                {
                    this.SerializeOther(value);
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                bool first = true;

                this.builder.Append('{');

                foreach (object e in obj.Keys)
                {
                    if (!first)
                    {
                        this.builder.Append(',');
                    }

                    this.SerializeString(e.ToString());
                    this.builder.Append(':');

                    this.SerializeValue(obj[e]);

                    first = false;
                }

                this.builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                this.builder.Append('[');

                bool first = true;

                foreach (object obj in array)
                {
                    if (!first)
                    {
                        this.builder.Append(',');
                    }

                    this.SerializeValue(obj);

                    first = false;
                }

                this.builder.Append(']');
            }

            private void SerializeString(string str)
            {
                this.builder.Append('\"');

                char[] charArray = str.ToCharArray();
                foreach (var c in charArray)
                {
                    switch (c)
                    {
                        case '"':
                            this.builder.Append("\\\"");
                            break;
                        case '\\':
                            this.builder.Append("\\\\");
                            break;
                        case '\b':
                            this.builder.Append("\\b");
                            break;
                        case '\f':
                            this.builder.Append("\\f");
                            break;
                        case '\n':
                            this.builder.Append("\\n");
                            break;
                        case '\r':
                            this.builder.Append("\\r");
                            break;
                        case '\t':
                            this.builder.Append("\\t");
                            break;
                        default:
                            int codepoint = Convert.ToInt32(c);
                            if ((codepoint >= 32) && (codepoint <= 126))
                            {
                                this.builder.Append(c);
                            }
                            else
                            {
                                this.builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                            }

                            break;
                    }
                }

                this.builder.Append('\"');
            }

            private void SerializeOther(object value)
            {
                if (value is float
                    || value is int
                    || value is uint
                    || value is long
                    || value is double
                    || value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is ulong
                    || value is decimal)
                {
                    this.builder.Append(value.ToString());
                }
                else
                {
                    this.SerializeString(value.ToString());
                }
            }
        }
    }
}
