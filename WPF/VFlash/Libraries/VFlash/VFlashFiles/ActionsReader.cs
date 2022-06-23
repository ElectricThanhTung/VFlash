using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Xml;
using VFlash;
using VFlash.Flashing;
using VFlash.ViewModel;

namespace VFlashFiles {
    internal class ActionsReader {
        private static Type[] FlashActionTypes = {
            typeof(StartDefaultSession),
            typeof(StartExtendedSession),
            typeof(CheckProgrammingPreconditions),
            typeof(DisableDTCSetting),
            typeof(DisableNormalCommunication),
            typeof(StartProgrammingSession),
            typeof(PerformSecurityAccess),
            typeof(WriteFingerPrint),
            typeof(FlashBinaries),
            typeof(CheckProgrammingDependencies),
            typeof(HardReset),
            typeof(EnableNormalCommunication),
            typeof(EnableDTCSetting),
            typeof(CustomRequest),
            typeof(DelayAction),
        };

        private string actionsPath;

        public ActionsReader(string actionsPath) {
            this.actionsPath = actionsPath;

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(actionsPath);

            if(xmlDocument.DocumentElement.Name != "FlashActions")
                throw CreateException("Root node is not FlashActions tag.");

            XmlAttribute alwaysExecutedAttr = xmlDocument.Attributes != null ? xmlDocument.Attributes["AlwaysExecuted"] : null;
            AlwaysExecuted = alwaysExecutedAttr != null ? alwaysExecutedAttr.Value.Trim().ToLower() == "true" : false;

            ObservableCollection <EcuViewModel> flashes = new ObservableCollection<EcuViewModel>();
            Actions = new List<FlashAction>();
            foreach(XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                Actions.Add(GetAction(node));
        }

        public bool AlwaysExecuted {
            get; private set;
        }

        public List<FlashAction> Actions {
            get; private set;
        }

        private Exception CreateException(string msg) {
            throw new Exception("An error occurred while reading \"" + Path.GetFileName(actionsPath) + "\" file. " + msg);
        }

        private void ActionSetValue(FlashAction action, string propertyName, object value) {
            PropertyInfo property = action.GetType().GetProperty(propertyName);
            if(property == null)
                throw CreateException(propertyName + " is an invalid attribute.");
            if(!property.CanWrite || !property.CanRead)
                throw CreateException(propertyName + " is an invalid attribute.");
            property.SetValue(action, value);
        }

        private byte[] GetArrayFormString(string s) {
            s = s.Replace(",", " ");
            while(s.Contains("  "))
                s = s.Replace("  ", " ");
            List<byte> ret = new List<byte>();
            string[] hexValues = s.Split(' ');
            foreach(string hex in hexValues) {
                int value = Number.HexToInt(hex);
                if(value > 255)
                    throw CreateException("Invalid input string.");
                ret.Add((byte)value);
            }
            return ret.ToArray();
        }

        private FlashAction GetAction(XmlNode actionNode) {
            foreach(Type actionType in FlashActionTypes) {
                if(actionNode.Name == actionType.Name) {
                    FlashAction action = (FlashAction)Activator.CreateInstance(actionType);

                    foreach(XmlAttribute attribute in actionNode.Attributes) {
                        if(attribute.Name == "AlwaysExecuted")
                            ActionSetValue(action, "AlwaysExecuted", attribute.Value.Trim().ToLower() == "true");
                        else if(attribute.Name == "IsFunctional")
                            ActionSetValue(action, "IsFunctional", attribute.Value.Trim().ToLower() == "true");
                        else if(attribute.Name == "SuppressBit")
                            ActionSetValue(action, "SuppressBit", attribute.Value.Trim().ToLower() == "true");
                        else if(attribute.Name == "EraseEnabled")
                            ActionSetValue(action, "EraseEnabled", attribute.Value.Trim().ToLower() == "true");
                        else if(attribute.Name == "Name")
                            ActionSetValue(action, "Name", attribute.Value.Trim());
                        else if(attribute.Name == "DataRequest")
                            ActionSetValue(action, "DataRequest", GetArrayFormString(attribute.Value));
                        else if(attribute.Name == "ExpectedResponse")
                            ActionSetValue(action, "ExpectedResponse", GetArrayFormString(attribute.Value));
                        else
                            throw CreateException(attribute.Name + " is an invalid attribute.");
                    }

                    return action;
                }
            }
            throw CreateException("Action Node \"" + actionNode.Name + "\" is invalid.");
        }
    }
}
