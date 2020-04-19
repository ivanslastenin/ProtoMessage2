using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProtoMessageOriginal
{

    public class ProtoMessage4 : IProtoMessage<ProtoMessage4>
    {
        private readonly List<(LazyString, LazyAttributeString)> _attributes = new List<(LazyString, LazyAttributeString)>();
        private readonly List<(LazyString, ProtoMessage4)> _subMessages = new List<(LazyString, ProtoMessage4)>();


        private readonly List<(LazyString, LazyAttributeString)> _attributesBy = new List<(LazyString, LazyAttributeString)>();


        public ProtoMessage4()
        {
        }

        private class LazyString
        {
            private readonly int _indexStart;
            private readonly int _indexEnd;
            private readonly string? _protoAsText;
            private string? _value;

            public string Value => _value ?? ParseAttributeValue();

            public LazyString(int indexStart, int indexEnd, string protoAsText)
            {
                _indexStart = indexStart;
                _indexEnd = indexEnd;
                _protoAsText = protoAsText;
            }

            private string ParseAttributeValue()
            {
                int start = _indexStart;

                _value = _protoAsText.Substring(start, _indexEnd - start);
                return _value;
            }

            public long Hash => ((long)_indexStart) << 31 & _indexEnd;
        }

        private class LazyAttributeString
        {
            private readonly int _indexStart;
            private readonly int _indexEnd;
            private readonly string? _protoAsText;
            private string? _value;

            public string Value => _value ?? ParseAttributeValue();

            public LazyAttributeString(int indexStart, int indexEnd, string protoAsText)
            {
                _indexStart = indexStart;
                _indexEnd = indexEnd;
                _protoAsText = protoAsText;
            }

            private string ParseAttributeValue()
            {
                int start = _indexStart + 1; // skip whitespace

                _value = _protoAsText.Substring(start, _indexEnd - start);
                if (_value[_value.Length - 1] == '\r')
                {
                    _value = _value.Substring(0, _value.Length - 1);
                }
                if (_value[0] == '"')
                {
                    _value = _value.Substring(1, _value.Length - 2);
                }
                return _value;
            }
        }

        public void Parse(string protoAsText)
        {
            var lengthToProcess = protoAsText.Length;

            Stack<ProtoMessage4> messagesStack = new Stack<ProtoMessage4>();
            var currentProtoMessage = this;

            List<(LazyString, LazyAttributeString)> currentMessageAttributes = currentProtoMessage._attributes;
            List<(LazyString, ProtoMessage4)> currentMessagSubMessages = currentProtoMessage._subMessages;

            var elementNameStartPosition = 0;

            var attributeNameStartPosition = 0;
            LazyString attributeName = null;

            var attributeValueStartPosition = 0;

            char char1;

            bool isLineStart = true;
            bool attributeValue = false;

            for (int i = 0; i < lengthToProcess; i++)
            {
                char1 = protoAsText[i];

                if (!attributeValue)
                {
                    if (char1 == ' ' && isLineStart)
                    {
                        attributeNameStartPosition = i + 1;
                        elementNameStartPosition = i + 1;

                        continue;
                    }
                    else if (char1 == ':')
                    {
                        attributeName = new LazyString(attributeNameStartPosition, i, protoAsText);

                        attributeValueStartPosition = i + 1;
                        attributeValue = true;

                        continue;
                    }
                    else if (char1 == '{')
                    {
                        messagesStack.Push(currentProtoMessage);
                        currentProtoMessage = new ProtoMessage4();
                        currentMessagSubMessages.Add((
                            new LazyString(elementNameStartPosition, i - 1, protoAsText),
                            currentProtoMessage));

                        currentMessageAttributes = currentProtoMessage._attributes;
                        currentMessagSubMessages = currentProtoMessage._subMessages;

                        continue;
                    }
                    else if (char1 == '}')
                    {
                        currentProtoMessage = messagesStack.Pop();
                        currentMessageAttributes = currentProtoMessage._attributes;
                        currentMessagSubMessages = currentProtoMessage._subMessages;

                        continue;
                    }

                    else
                    {
                        isLineStart = false;
                    }
                }

                if (char1 == '\n')
                {
                    if (attributeName != null)
                    {
                        currentMessageAttributes.Add((attributeName, new LazyAttributeString(attributeValueStartPosition, i, protoAsText)));
                        attributeName = null;
                    }

                    attributeNameStartPosition = i + 1;
                    elementNameStartPosition = i + 1;

                    isLineStart = true;
                    attributeValue = false;
                }
            }
        }

        public List<ProtoMessage4> GetElementList(string name)
        {
            return _subMessages.Where(item => item.Item1.Value == name).Select(item => item.Item2).ToList();
        }

        public ProtoMessage4 GetElement(string name)
        {
            for (int i = 0; i < _subMessages.Count; i++)
            {
                var subMessages = _subMessages[i];
                if (subMessages.Item1.Value == name)
                {
                    return subMessages.Item2;
                }
            }
            return null;
        }

        public List<string> GetAttributeList(string name)
        {
            return _attributes.Where(item => item.Item1.Value == name).Select(item => item.Item2.Value).ToList();
        }

        public T GetAttribute<T>(string name) where T : struct
        {
            string attr = GetAttribute(name);
            return (T)Convert.ChangeType(attr, typeof(T), CultureInfo.InvariantCulture);
        }

        public T? GetAttributeOrNull<T>(string name) where T : struct
        {
            string attr = GetAttribute(name);
            if (attr == null)
            {
                return null;
            }

            return (T)Convert.ChangeType(attr, typeof(T), CultureInfo.InvariantCulture);
        }

        public string GetAttribute(string name)
        {
            for(int i = 0; i < _attributes.Count; i++)
            {
                var attribute = _attributes[i];
                if (attribute.Item1.Value == name)
                {
                    return attribute.Item2.Value;
                }
            }
            return null;
        }

        public List<string> GetKeys()
        {
            var res = new List<string>();

            void GetSubMessages(List<(LazyString, ProtoMessage4)> messages)
            {
                foreach (var msg in messages)
                {
                    res.Add(msg.Item1.Value);
                    GetSubMessages(msg.Item2._subMessages);
                }
            }

            GetSubMessages(_subMessages);

            return res;
        }
    }
}
