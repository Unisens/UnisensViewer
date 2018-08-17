using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NLog;
using UnisensViewer.Helpers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using UnisensViewerLibrary;
using System.Diagnostics;

namespace UnisensViewer
{
    public static class RendererManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static Hashtable renderers;
        private static Hashtable renderersReversehash;

        private static double time;
        private static double timestretch;
        private static double timemax;
        private static double maxbreite;
        private static int imagewidth;

        private static EventHandler rkillhandler = new EventHandler(RendererKill);

        private static bool sincinterpolation;
        private static bool peaksamples;
        private static bool crhair;
        private static bool point;
        private static bool sampleandhold;
        private static bool linear;

        static RendererManager()
        {
            logger.Debug("Initialize RenderManager");

            renderers = new Hashtable();
            renderersReversehash = new Hashtable();

            time = 0.0;
            timestretch = 10.0;

            peaksamples = true;
            // von screenwidth abziehen:
            // - den scroller vom signalstacker
            // - den gridsplitter zwischen dem xmlcontrol und dem signalstacker
            // - die y-achsen-controls brauchen auch noch platz
            maxbreite = SystemParameters.PrimaryScreenWidth
                        - SystemParameters.VerticalScrollBarWidth
                        - 5.0
                        - (5.0 * 16.0);
            imagewidth = 128;
        }

        public static event EventHandler TimeChanged;

        public static event EventHandler TimeMaxChanged;

        public static event EventHandler TimeStretchChanged;

        public static bool Point
        {
            get { return point; }
        }

        public static bool SampleAndHold
        {
            get { return sampleandhold; }
        }

        public static bool Linear
        {
            get { return linear; }
        }

        public static bool Fadenkreuz
        {
            get { return crhair; }
        }

        public static bool SincInterpolation
        {
            get { return sincinterpolation; }
        }

        public static bool PeakSamples
        {
            get { return peaksamples; }
        }

        public static double TimeMax
        {
            get { return timemax; }
        }

        public static double Time
        {
            get
            {
                return time;
            }

            private set
            {
                if (value < 0.0)
                {
                    time = 0.0;
                }
                else if (value <= timemax)
                {
                    time = value;
                }
                else
                {
                    time = timemax;
                }
            }
        }

        public static double TimeStretch
        {
            get
            {
                return timestretch;
            }

            private set
            {
                if (value > 0.001)
                {
                    timestretch = value;
                }
                else
                {
                    timestretch = 0.001;
                }
            }
        }

        public static double MaxBreite
        {
            get
            {
                return maxbreite;
            }

            private set
            {
                if (value > 0.0)
                {
                    maxbreite = value;
                }
                else
                {
                    maxbreite = 0.0;
                }
            }
        }

        public static void Render()
        {
            using (new WaitCursor())
            {
                foreach (DictionaryEntry d in renderers)
                {
                    ((Renderer)d.Value).Render(time, timestretch, null);
                }
            }
        }

        public static void Render(Renderer r, XElement channel)
        {
            using (new WaitCursor())
            {
                r.Render(time, timestretch, channel);
            }
        }

        public static void Scroll(double time)
        {
            Time = time;
            Render();

            TimeChanged(null, null);
        }

        public static void Stretch(double timestretch)
        {
            TimeStretch = timestretch;
            Render();

            TimeStretchChanged(null, null);
        }      
        /// <summary>
        /// new Render in X-direchtion
        /// </summary>
        /// <param name="stackercontrol"></param>
        /// <param name="imageHeight">New image height in pixels (height is the time or x axis</param>
        public static void Drag(StackerControl stackercontrol, double imageHeight)
        {
            Trace.WriteLine("-> Drag(StackerControl stackercontrol, double imageHeight) (maxbreite)" + imageHeight);


            if (imageHeight < 20)
            {
                imageHeight = 20;
            }
            MaxBreite = imageHeight;            
            for (int i = 0; i < stackercontrol.renderSliceLists.Count; i++)
            {
                RenderSlice rs = null;

                // modify all RenderSlice of the Renderers
                for (int j = 0; j < stackercontrol.renderSliceLists[i].Count; j++)
                {                                  
                    // at adding a new RenderSlice in the rslist, 
                    // the current RenderSlice is moved forward.
                    rs = stackercontrol.renderSliceLists[i][j];         
                    int imageWidth = ((RasterRenderSlice)rs).ImageWidth;

                    Trace.WriteLine("Drag: Renderer_Update(rs, imageWidth, imageHeight); " + imageWidth + " and " + MaxBreite);

                    Renderer_Update(rs, imageWidth, MaxBreite);
                    RenderSlice_Update(rs, imageWidth, MaxBreite);

                    // old RenderSlice replaced by new
                    Renderer r = rs.Renderer;
                    XElement channel = rs.UnisensNode;

                    //replaced with correct RenderSlice OnPropertyChanged("ImageSource");
                    //stackercontrol.MoveRenderSlice(rs, stackercontrol.renderSliceLists[i]);

                    // paint signal again
                    RendererManager.Render(r, channel);
                }
            }          
        }

        /// <summary>
        /// Updates the Renderer of a specific RenderSlice with a new width and new height. 
        /// Width and height are mixed up, because the bitmap is rendered in rotation of 90°.
        /// </summary>
        /// <param name="rs">Change the dimensions of this RenderSlice</param>
        /// <param name="imageWidth">New image width in pixels (width is the unit or y axis)</param>
        /// <param name="imageHeight">New image height in pixels (height is the time or x axis)</param>
        public static void Renderer_Update(RenderSlice rs, int imageWidth, double imageHeight)
        {
            Trace.WriteLine("Renderer_Update(RenderSlice rs, int imageWidth, double imageHeight)" + imageWidth + " and " + imageHeight);

            switch (rs.Renderer.ToString())
            {
                case "UnisensViewer.StreamRenderer":
                    ((StreamRenderer)rs.Renderer).imageheight = (int)imageHeight;
                    ((StreamRenderer)rs.Renderer).dirtyrect.Height = (int)imageHeight;
                    ((StreamRenderer)rs.Renderer).imagewidth = imageWidth;
                    ((StreamRenderer)rs.Renderer).dirtyrect.Width = imageWidth;
                    break;
                case "UnisensViewer.EventStringRenderer":
                    ((EventStringRenderer)rs.Renderer).imageheight = (int)imageHeight;
                    ((EventStringRenderer)rs.Renderer).dirtyrect.Height = (int)imageHeight;
                    ((EventStringRenderer)rs.Renderer).imagewidth = imageWidth;
                    ((EventStringRenderer)rs.Renderer).dirtyrect.Width = imageWidth;
                    break;
                case "UnisensViewer.EventValueRenderer":
                    ((EventValueRenderer)rs.Renderer).imageheight = (int)imageHeight;
                    ((EventValueRenderer)rs.Renderer).dirtyrect.Height = (int)imageHeight;
                    ((EventValueRenderer)rs.Renderer).imagewidth = imageWidth;
                    ((EventValueRenderer)rs.Renderer).dirtyrect.Width = imageWidth;
                    break;
            }
        }

        /// <summary>
        /// Updates a RenderSlice with a new width and new height. 
        /// Width and height are mixed up, because the bitmap is rendered in rotation of 90°.
        /// This method changes all information of the RenderSlice.
        /// </summary>
        /// <param name="rs">Change the dimensions of this RenderSlice</param>
        /// <param name="imageWidth">New image width in pixels (width is the unit or y axis)</param>
        /// <param name="imageHeight">New image height in pixels (height is the time or x axis)</param>
        public unsafe static void RenderSlice_Update(RenderSlice rs, int imageWidth, double imageHeight)
        {
            Trace.WriteLine("RenderSlice_Update(RenderSlice rs, int imageWidth, double imageHeight)" + imageWidth + " and " + imageHeight);

            DrawingGroup drawinggroup = new DrawingGroup();
            ((RasterRenderSlice)rs).ImageHeight = (int)imageHeight;
            ((RasterRenderSlice)rs).ImageWidth = imageWidth;
            ((RasterRenderSlice)rs).WBmp = new WriteableBitmap(imageWidth, (int)imageHeight, 96, 96, PixelFormats.Pbgra32, null);
            ((RasterRenderSlice)rs).argb = (uint*)((RasterRenderSlice)rs).WBmp.BackBuffer;
            ((RasterRenderSlice)rs).strideUInt32 = ((RasterRenderSlice)rs).WBmp.BackBufferStride >> 2;

            Rect rect = new Rect(0.0, 0.0, imageWidth, ((RasterRenderSlice)rs).ImageHeight);
            ImageDrawing imagedrawing = new ImageDrawing(((RasterRenderSlice)rs).WBmp, rect);

            MatrixTransform mt = new MatrixTransform(0.0, 1.0, -1.0, 0.0, imageWidth, 0);
            mt.Freeze();

            drawinggroup.Children.Add(imagedrawing);
       
            switch (rs.Renderer.ToString())
            {
                case "UnisensViewer.EventStringRenderer":
                    ((EventRenderSlice)rs).geometrygroup.Transform = mt;
                    drawinggroup.Children.Add(((EventRenderSlice)rs).geometrydrawing);
                    break;
                case "UnisensViewer.EventValueRenderer":
                    ((RasterRenderSlice)rs).geometrygroup.Transform = mt;
                    drawinggroup.Children.Add(((RasterRenderSlice)rs).geometrydrawing);
                    break;
                default:
                    break;
            }
            drawinggroup.ClipGeometry = new RectangleGeometry(rect);
            //((RasterRenderSlice)rs).imagesource = new DrawingImage(drawinggroup);
            rs.ImageSource = new DrawingImage(drawinggroup);
        }

        public static void SetInterpolation(bool usesinc)
        {
            sincinterpolation = usesinc;
            Render();
        }

        public static void SetPeak(bool usepeak)
        {
            peaksamples = usepeak;
            Render();
        }

        public static void SetSampleAndHold(bool usesampleandhold)
        {
            point = false;
            linear = false;
            sampleandhold = usesampleandhold;
            Render();
        }

        public static void SetPoint(bool usepoint)
        {
            sampleandhold = false;
            linear = false;
            point = usepoint;
            Render();
        }

        public static void SetLinear(bool uselinear)
        {
            point = false;
            sampleandhold = false;
            linear = uselinear;
            Render();
        }

        public static void SetFadenkreuz(bool usecrhair)
        {
            crhair = usecrhair;
        }

        public static void KillRenderer(XElement sev_entry)
        {
            XAttribute id = sev_entry.Attribute("id");

            if (id != null)
            {
                string key = id.Value.ToLowerInvariant();

                Renderer r = (Renderer)renderers[key];
                if (r != null)
                {
                    r.RaiseKill();
                }
            }
        }

        public static void AutoZoomGroupedByFiles(IEnumerable<RenderSlice> rslist)
        {
            IEnumerable<Renderer> rlist = GetInvolvedRenderers(rslist);
            UpdateZoomInfo(rlist);

            // nach renderer gruppieren, zoominfos mergen und zoomen
            // man kann nicht einfach alle kinder von nem renderer nehmen, da nicht unbedingt alle im in der rslist sind
            foreach (Renderer r in rlist)
            {
                IEnumerable<RenderSlice> rsr = from rs in rslist
                                               where rs.Renderer == r
                                               select rs;
                MergedZoomInto(rsr);
            }

            UpdateRenderers(rlist);
        }

        public static void CloseRenderer(XElement sev_entry)
        {
            XAttribute id = sev_entry.Attribute("id");

            if (id != null)
            {
                string key = id.Value.ToLowerInvariant();

                Renderer r = (Renderer)renderers[key];
                if (r != null)
                {
                    r.Close();
                }
            }
        }

        public static void AutoZoomIndividual(IEnumerable<RenderSlice> rslist)
        {
            IEnumerable<Renderer> rlist = GetInvolvedRenderers(rslist);
            UpdateZoomInfo(rlist);

            // alle rs zoomen
            foreach (RenderSlice rs in rslist)
            {
                if (rs.Zoominfo.PhysicalMin != float.MaxValue)
                {
                    // zoominfo nicht initialisiert / keine daten?
                    rs.ZoomInto(rs.Zoominfo.PhysicalMin, rs.Zoominfo.PhysicalMax);
                }
                else
                {
                    rs.ZoomInto(0.0f, 0.0f);
                }
            }

            UpdateRenderers(rlist);
        }

        public static void ReOpenRenderer(XElement sev_entry)
        {
            XAttribute id = sev_entry.Attribute("id");

            if (id != null)
            {
                string key = id.Value.ToLowerInvariant();

                Renderer r = (Renderer)renderers[key];
                if (r != null)
                {
                    r.ReOpen();
                }
            }
        }

        public static Renderer GetRenderer(XElement sev_entry)	// signalEntry, eventEntry, valueEntry
        {
            XAttribute id = sev_entry.Attribute("id");

            if (id != null)
            {
                string key = id.Value.ToLowerInvariant();
                Renderer r = (Renderer)renderers[key];

                if (r == null)
                {
                    r = CreateRenderer(sev_entry);

                    if (r != null)
                    {
                        renderers[key] = r;
                        renderersReversehash[r] = key;

                        UpdateTimeMax();
                    }
                }

                return r;
            }
            else
            {
                return null;
            }
        }

        public static void AutoZoomGroupedByUnits(IEnumerable<RenderSlice> rslist)
        {
            IEnumerable<Renderer> rlist = GetInvolvedRenderers(rslist);
            UpdateZoomInfo(rlist);

            Hashtable group = GroupByUnits(rslist);		// keys sind entweder unit-strings oder scale-arrays

            foreach (DictionaryEntry d in group)
            {
                if (d.Key is string)
                {
                    MergedZoomInto((IEnumerable<RenderSlice>)d.Value);
                }
                else
                {
                    // ist ein scale-array, also object[]
                    ScaledZoomInto((IEnumerable<RenderSlice>)d.Value, (object[])d.Key);
                }
            }

            UpdateRenderers(rlist);
        }

        public static IEnumerable<XElement> GetSevEntries(IList renderslicelist)
        {
            List<XElement> l = new List<XElement>();

            foreach (RenderSlice rs in renderslicelist)
            {
                if (!l.Contains(rs.Renderer.SevEntry))
                {
                    l.Add(rs.Renderer.SevEntry);
                }
            }

            return l;
        }

        public static IEnumerable<XElement> GetSevEntriesAllRenderers()
        {
            List<XElement> l = new List<XElement>();

            foreach (DictionaryEntry d in renderers)
            {
                l.Add(((Renderer)d.Value).SevEntry);
            }

            return l;
        }

        public static IEnumerable<Renderer> GetInvolvedRenderers(IEnumerable<RenderSlice> rslist)
        {
            // alle beteiligten renderer ermitteln
            List<Renderer> rl = new List<Renderer>();

            foreach (RenderSlice rs in rslist)
            {
                if (!rl.Contains(rs.Renderer))
                {
                    rl.Add(rs.Renderer);
                }
            }

            return rl;
        }

        public static void UpdateTimeMax()
        {
            timemax = 0.0;

            foreach (DictionaryEntry d in renderers)
            {
                double t = ((Renderer)d.Value).TimeMax;

                if (t > timemax)
                {
                    timemax = t;
                }
            }

            TimeMaxChanged(null, null);
        }

        private static Renderer CreateRenderer(XElement sev_entry)
        {
            //// von screenwidth abziehen:
            //// - den scroller vom signalstacker
            //// - den gridsplitter zwischen dem xmlcontrol und dem signalstacker
            //// - die y-achsen-controls brauchen auch noch platz
            //double maxbreite = SystemParameters.PrimaryScreenWidth
            //                    - SystemParameters.VerticalScrollBarWidth
            //                    - 5.0
            //                    - (5.0 * 16.0);

            // XXX wenn das imagecontrol nicht gestaucht wird, sondern "reingefahren", dann
            //     nur PrimaryScreenWidth übergeben.
            //     Das StackerControl soll dann den DesiredSize-Wert an den Renderer übergeben.
            try
            {
                Renderer r = null;

                switch (sev_entry.Name.LocalName)
                {
                    case "signalEntry":
                        r = new StreamRenderer(sev_entry, (int)maxbreite, imagewidth);
                        break;
                    case "eventEntry":
                        r = new EventStringRenderer(sev_entry, (int)maxbreite, imagewidth);
                        break;
                    case "valuesEntry":
                        r = new EventValueRenderer(sev_entry, (int)maxbreite, imagewidth);
                        break;
                    default:
                        return null;
                }

                r.Kill += rkillhandler;
                return r;
            }
            catch (Exception e)
            {
                //MessageBox.Show("Ist ein Attribut ungültig oder nicht vorhanden?\n" + e.Message);
                logger.ErrorException("Ist ein Attribut ungültig oder nicht vorhanden?", e);

                return null;
            }
        }

        private static void RendererKill(object sender, EventArgs e)
        {
            Renderer r = (Renderer)sender;

            r.Kill -= rkillhandler;

            // renderer löschen (muss aber erstmal den key dazu haben...)
            string key = (string)renderersReversehash[r];

            renderers.Remove(key);
            renderersReversehash.Remove(r);

            UpdateTimeMax();
        }

        private static void UpdateZoomInfo(IEnumerable<Renderer> rlist)
        {
            using (new WaitCursor())
            {
                // zoominfo auf allen beteiligten renderern updaten
                foreach (Renderer r in rlist)
                {
                    r.UpdateZoomInfo(time, timestretch);
                }
            }
        }

        public static void UpdateRenderers(IEnumerable<Renderer> rlist)
        {
            using (new WaitCursor())
            {
                foreach (Renderer r in rlist)
                {
                    r.Render(time, timestretch, null);
                }
            }
        }

        public static void MergedZoomInto(RenderSlice rs)
        {
            ZoomInfo zi = new ZoomInfo(float.MaxValue, float.MinValue);

            if (rs.Zoominfo.PhysicalMax > zi.PhysicalMax)
            {
                zi.PhysicalMax = rs.Zoominfo.PhysicalMax;
            }

            if (rs.Zoominfo.PhysicalMin < zi.PhysicalMin)
            {
                zi.PhysicalMin = rs.Zoominfo.PhysicalMin;
            }

            if (rs.Zoominfo.PhysicalMin != float.MaxValue)
            {
                // zoominfo nicht initialisiert / keine daten?
                rs.ZoomInto(zi.PhysicalMin, zi.PhysicalMax);
            }
            else
            {
                rs.ZoomInto(0.0f, 0.0f);
            }
        }

        //public static void MergedZoomInto(IEnumerable<RenderSlice> rslist)
        //{
        //    foreach (RenderSlice rs in rslist)
        //    {
        //        MergedZoomInto(rs);
        //    }
        //}

        public static void MergedZoomInto(IEnumerable<RenderSlice> rslist)
        {
            ZoomInfo zi = new ZoomInfo(float.MaxValue, float.MinValue);

            foreach (RenderSlice rs in rslist)
            {
                if (rs.Zoominfo.PhysicalMax > zi.PhysicalMax)
                {
                    zi.PhysicalMax = rs.Zoominfo.PhysicalMax;
                }

                if (rs.Zoominfo.PhysicalMin < zi.PhysicalMin)
                {
                    zi.PhysicalMin = rs.Zoominfo.PhysicalMin;
                }
            }

            foreach (RenderSlice rs in rslist)
            {
                if (rs.Zoominfo.PhysicalMin != float.MaxValue)
                {
                    // zoominfo nicht initialisiert / keine daten?
                    rs.ZoomInto(zi.PhysicalMin, zi.PhysicalMax);
                }
                else
                {
                    rs.ZoomInto(0.0f, 0.0f);
                }
            }
        }

        private static Hashtable GroupByUnits(IEnumerable<RenderSlice> rslist)
        {
            Hashtable group = new Hashtable(new AutoZoomGroupEqualityComparer());

            // gruppierung erfolgt folgendermassen:
            // - es wird geschaut, welche renderslices das gleiche skalen-array verwenden (in Units aus settings.xml geparst).
            // - falls eine unbekannte einheit verwendet wurde (d.h. keine skalen-array definition vorhanden), dann nach dem string gruppieren
            foreach (RenderSlice rs in rslist)
            {
                object key = "no unit";		// rs ohne unit werden in diese gruppe gepackt

                if (rs.Unit != null)
                {
                    object[] s = (object[])Units.UInitIndex[rs.Unit];
                    key = s != null ? (object)s : (object)rs.Unit;
                }

                if (group.Contains(key))
                {
                    ((List<RenderSlice>)group[key]).Add(rs);
                }
                else
                {
                    List<RenderSlice> l = new List<RenderSlice>();
                    l.Add(rs);
                    group[key] = l;
                }
            }

            return group;
        }

        private static void ScaledZoomInto(IEnumerable<RenderSlice> rslist, object[] scale)
        {
            List<AutoZoomGroup> zoomgroups = new List<AutoZoomGroup>();
            double f = 0.0;

            // größte benutzte unit (d.h. erste im scale-array) ist referenz, alles andere wird dazu skaliert.
            // renderslices nach si-prefixen sortieren, dabei umrechnungsfaktoren merken.
            for (int a = 0; a < scale.Length; ++a)
            {
                if (scale[a] is string)
                {
                    // fix: synonyme einheiten
                    string[] u = ((string)scale[a]).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    IEnumerable<RenderSlice> rsx = from rs in rslist
                                                   where u.Contains(rs.Unit)
                                                   select rs;

                    if (rsx.Count() != 0)
                    {
                        if (f == 0.0)
                        {
                            f = 1.0;
                        }

                        zoomgroups.Add(new AutoZoomGroup(rsx, f));
                    }
                }
                else
                {
                    f *= (double)scale[a];
                }
            }

            // liste von zoomgroups beginnt mit der höchsten si-prefix-einheit (erste gruppe hat scalefactor = 1.0).
            // jetzt ein zoominfo mergen, mit entsprechenden skalierungen
            ZoomInfo zi = new ZoomInfo(float.MaxValue, float.MinValue);

            foreach (AutoZoomGroup zg in zoomgroups)
            {
                foreach (RenderSlice rs in zg.RsList)
                {
                    float scaledmax = (float)(rs.Zoominfo.PhysicalMax / zg.ScaleFactor);
                    float scaledmin = (float)(rs.Zoominfo.PhysicalMin / zg.ScaleFactor);

                    if (scaledmax > zi.PhysicalMax)
                    {
                        zi.PhysicalMax = scaledmax;
                    }

                    if (scaledmin < zi.PhysicalMin)
                    {
                        zi.PhysicalMin = scaledmin;
                    }
                }
            }

            // jetzt skaliert reinzoomen
            foreach (AutoZoomGroup zg in zoomgroups)
            {
                foreach (RenderSlice rs in zg.RsList)
                {
                    if (rs.Zoominfo.PhysicalMin != float.MaxValue)
                    {
                        // zoominfo nicht initialisiert / keine daten?
                        rs.ZoomInto((float)(zi.PhysicalMin * zg.ScaleFactor), (float)(zi.PhysicalMax * zg.ScaleFactor));
                    }
                    else
                    {
                        rs.ZoomInto(0.0f, 0.0f);
                    }
                }
            }
        } 
    }
}

#region old code
/*
public static void ZoomInto()
{
	foreach (DictionaryEntry d in renderers)
		((Renderer)d.Value).ZoomInto(time, timestretch);
}
*/

#endregion