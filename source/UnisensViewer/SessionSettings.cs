using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnisensViewer.DataStructs;
using UnisensViewerLibrary;

namespace UnisensViewer
{

    public class SessionView
    {
        private string _path;
        private string _name;

        public SessionView(string path)
        {
            _path = path;
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string Name
        {
            get { return _path.Substring(2, _path.Length-7); }
        }

    }

    public class RenderDataModel
    {
        private int _imageheight;
        private int _imagewidth;
        private float _scale;
        private float _offset;
        private double _range;
        private int _posY;
        private int _posX;

        [XmlElement]
        public int ImageHeight
        {
            get { return _imageheight; }
            set { _imageheight = value; }
        }

        [XmlElement]
        public int ImageWidth
        {
            get { return _imagewidth; }
            set { _imagewidth = value; }
        }


        [XmlElement]
        public float Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        [XmlElement]
        public float Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        [XmlElement]
        public double Range
        {
            get { return _range; }
            set { _range = value; }
        }

        [XmlElement]
        public int PosX
        {
            get { return _posX; }
            set { _posX = value; }
        }

        [XmlElement]
        public int PosY
        {
            get { return _posY; }
            set { _posY = value; }
        }
    }



    [XmlSerializerFormat]
    [XmlRoot("SessionSettings")]
    public class SessionSettings
    {
        private bool _emptySettings = false;
        private string _path;
        private double _time;
        private double _timeStretch;
        private SerializableDictionary<string, RenderDataModel> _activeEntries;
        private SignalViewerControl _signalviewercontrol;

        [XmlElement]
        public SerializableDictionary<string, RenderDataModel> ActiveEntries
        {
            get { return _activeEntries; }
            set { _activeEntries = value; }
        }

        [XmlElement]
        public double Time
        {
            get { return _time; }
            set { _time = value; }
        }

        [XmlElement]
        public double TimeStretch
        {
            get { return _timeStretch; }
            set { _timeStretch = value; }
        }

        private static SessionSettings _sessionSettings;
        
        public static SessionSettings Instance 
        {
            get
            {
                return _sessionSettings;
            }
        }

        private SessionSettings()
        {
            _activeEntries = new SerializableDictionary<string, RenderDataModel>();
            
        }


        public static SessionSettings Load(SignalViewerControl signalviewercontrol, SessionView view)
        {
                 
            _sessionSettings = GetObjectFrom(view.Path);
            _sessionSettings._signalviewercontrol = signalviewercontrol;

            return _sessionSettings;
        }



        public void Update()
        {

            Time = RendererManager.Time;
            TimeStretch = RendererManager.TimeStretch;

            _activeEntries.Clear();

            int i = 0;
            foreach (var list in _signalviewercontrol.stackercontrol.renderSliceLists)
            {
                int j = 0;
                foreach (var item in list)
                {
                    RenderDataModel entry;
                    if (!_activeEntries.TryGetValue(ValueEntry.GetId(item.Renderer.SevEntry), out entry))
                    {
                        entry = new RenderDataModel();
                        _activeEntries.Add(ValueEntry.GetId(item.Renderer.SevEntry), entry);
                    }

                    entry.ImageWidth = (int) item.ImageSource.Width;

                    entry.Offset = item.Offset;
                    entry.Range = item.Range;
                    entry.Scale = item.Scale;

                    entry.PosX = i;
                    entry.PosY = j;

                    j++;
                }
                i++;
            }
        }

        public void Apply()
        {
            if (EmptySettings)
                return;

            SortedDictionary<int, SortedDictionary<int, RenderSlice>> sortedSlices = new SortedDictionary<int, SortedDictionary<int, RenderSlice>>();

            foreach (var list in _signalviewercontrol.stackercontrol.renderSliceLists)
            {
                foreach (var item in list)
                {
                    RenderDataModel renderDataModel;

                    if(_activeEntries.TryGetValue(ValueEntry.GetId(item.Renderer.SevEntry), out renderDataModel))
                    {
                        SortedDictionary<int, RenderSlice> value;

                        if (!sortedSlices.TryGetValue(renderDataModel.PosX, out value))
                        {
                            value = new SortedDictionary<int, RenderSlice>();
                            sortedSlices.Add(_activeEntries[ValueEntry.GetId(item.Renderer.SevEntry)].PosX, value);
                        }

                        value.Add(_activeEntries[ValueEntry.GetId(item.Renderer.SevEntry)].PosY, item);
                    }
                }
            }





            _signalviewercontrol.stackercontrol.renderSliceLists.Clear();

            foreach (var list in sortedSlices.Values)
            {
                var newlist = new ObservableCollection<RenderSlice>();

                foreach (var item in list.Values)
                {
                    newlist.Add(item);

                    var entry = _activeEntries[ValueEntry.GetId(item.Renderer.SevEntry)];

                    item.Offset = entry.Offset;
                    item.Range = entry.Range;

                    RendererManager.Renderer_Update(item, entry.ImageWidth, item.ImageSource.Height);
                    RendererManager.RenderSlice_Update(item, entry.ImageWidth, item.ImageSource.Height);
                    
                    item.Scale = (float)(entry.ImageWidth / item.Range);             

                    RendererManager.Render(item.Renderer, item.UnisensNode);
                }

                _signalviewercontrol.stackercontrol.renderSliceLists.Add(newlist);
            }

            sortedSlices.Clear();

            RendererManager.Scroll(Time);
            RendererManager.Stretch(TimeStretch);
        }

        

        [XmlIgnore]
        public bool EmptySettings
        {
            get { return _emptySettings; }
            set { _emptySettings = value; }
        }

        private static SessionSettings GetObjectFrom(String filename)
        {
            string path = Path.Combine(Environment.CurrentDirectory, filename); 

            try
            {
                using (var reader = XmlTextReader.Create(path))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(SessionSettings));
                    SessionSettings m = (SessionSettings)deserializer.Deserialize(reader);
                    m._path = path;
                    return m;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Es ist ein Fehler beim Laden der Konfigurationsdatei aufgetreten, Konfiguration wird neu erstellt: " + ex.Message, "Fehler");

                SessionSettings settings = new SessionSettings();
                settings._path = path;
                settings.EmptySettings = true;

                return settings;
            }
        }


        public void WriteObject()
        {
            if (Properties.Settings.Default.SessionSettings)
            {
                try
                {
                    using (XmlTextWriter xml = new XmlTextWriter(_path, Encoding.UTF8))
                    {
                        var writer = new XmlSerializer(typeof(SessionSettings));
                        xml.Formatting = Formatting.Indented;
                        writer.Serialize(xml, this);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Es ist ein Fehler beim speichern der Konfigurationsdatei aufgetreten, Konfiguration wird neu erstellt: " + ex.Message, "Fehler");
                }
            }
        }

    }
}
