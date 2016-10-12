﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class GraphItWindow : EditorWindow
{
    static Vector2 mScrollPos;
    static float mWidth;

    static int mMouseOverGraphIndex = -1;
    static float mMouseX = 0;

    static float x_offset = 2.0f;
    static float y_gap = 5.0f;
    static float y_offset = 2;
    static int precision_slider = 3;

    static GUIStyle NameLabel;
    static GUIStyle SmallLabel;
    static GUIStyle HoverText;
    static GUIStyle FracGS;

    static Material mLineMaterial;
    static void InitializeStyles()
    {
        if (NameLabel == null)
        {
            NameLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
            NameLabel.normal.textColor = Color.white;
            SmallLabel = new GUIStyle(EditorStyles.whiteLabel);
            SmallLabel.normal.textColor = Color.white;

            HoverText = new GUIStyle(EditorStyles.whiteLabel);
            HoverText.alignment = TextAnchor.UpperRight;
            HoverText.normal.textColor = Color.white;

            FracGS = new GUIStyle(EditorStyles.whiteLabel);
            FracGS.alignment = TextAnchor.LowerLeft;
        }
    }

    [MenuItem("Window/GraphIt")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        GraphItWindow window = (GraphItWindow)EditorWindow.GetWindow(typeof(GraphItWindow), false, "GraphIt " + GraphIt.VERSION);
        window.minSize = new Vector2(230f, 250f);
        window.Show();
    }

    void OnEnable()
    {
        EditorApplication.update += MyDelegate;
    }

    void OnDisable()
    {
        EditorApplication.update -= MyDelegate;
    }

    void MyDelegate()
    {
        Repaint();
    }

    static void DrawGraphGridLines(float y_pos, float width, float height, bool draw_mouse_line)
    {
        GL.Color(new Color(0.3f, 0.3f, 0.3f));
        float steps = 8;
        float x_step = width / steps;
        float y_step = height / steps;
        for (int i = 0; i < steps + 1; ++i)
        {
            Plot(x_offset + x_step * i, y_pos, x_offset + x_step * i, y_pos + height);
            Plot(x_offset, y_pos + y_step * i, x_offset + width, y_pos + y_step * i);
        }

        GL.Color(new Color(0.4f, 0.4f, 0.4f));
        steps = 4;
        x_step = width / steps;
        y_step = height / steps;
        for (int i = 0; i < steps + 1; ++i)
        {
            Plot(x_offset + x_step * i, y_pos, x_offset + x_step * i, y_pos + height);
            Plot(x_offset, y_pos + y_step * i, x_offset + width, y_pos + y_step * i);
        }

        if (draw_mouse_line)
        {
            GL.Color(new Color(0.8f, 0.8f, 0.8f));
            Plot(mMouseX, y_pos, mMouseX, y_pos + height);
        }
    }

    static void Plot(float x0, float y0, float x1, float y1)
    {
        GL.Vertex3(x0, y0, 0);
        GL.Vertex3(x1, y1, 0);
    }

    static void DrawDataRect(float x0, float y0, float x1, float y1,float yBottom)
    {
        GL.Begin(GL.QUADS);
        GL.Color(Color.yellow);
        GL.Vertex3(x0, y0, 0);
        GL.Vertex3(x1, y1, 0);
        GL.Vertex3(x1, yBottom, 0);
        GL.Vertex3(x0, yBottom, 0);
        GL.End();
    }

    static void CreateLineMaterial()
    {
        if (!mLineMaterial)
        {
            mLineMaterial = new Material(Shader.Find("Custom/GraphIt"));
            mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
            mLineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    // Use this for initialization
    void OnGUI()
    {
        DrawGraphs(position, this);
    }

    public static void DrawGraphs(Rect rect, EditorWindow window)
    {
        if (GraphIt.Instance)
        {
            InitializeStyles();
            CreateLineMaterial();

            EditorGUILayout.BeginHorizontal(GUIStyle.none);
            EditorGUILayout.BeginVertical(FracGS, GUILayout.Height(28));
            precision_slider = EditorGUILayout.IntSlider("Fractional Digits", precision_slider, 0, 15);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Show All Graphs"))
            {
                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    kv.Value.SetHidden(false);
                }
            }
            /*
            if (GUILayout.Button("?"))
            {
                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    kv.Value.SetHidden(false);
                }
            }*/
            //EditorGUILayout.LabelField("Left click+drag on graph to resize. Right click to hide graph.", EditorStyles.helpBox );
            EditorGUILayout.EndHorizontal();
            mLineMaterial.SetPass(0);

            int graph_index = 0;

            //use this to get the starting y position for the GL rendering
            Rect find_y = EditorGUILayout.BeginVertical(GUIStyle.none);
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                GL.PushMatrix();
                float start_y = find_y.y;
                GL.Viewport(new Rect(0, 0, rect.width, rect.height - start_y));
                GL.LoadPixelMatrix(0, rect.width, rect.height - start_y, 0);


                //Draw grey BG
                GL.Begin(GL.QUADS);
                GL.Color(new Color(0.2f, 0.2f, 0.2f));

                float scrolled_y_pos = y_offset - mScrollPos.y;
                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    if (kv.Value.GetHidden())
                    {
                        continue;
                    }

                    float height = kv.Value.GetHeight();
                    GL.Vertex3(x_offset, scrolled_y_pos, 0);
                    GL.Vertex3(x_offset + mWidth, scrolled_y_pos, 0);
                    GL.Vertex3(x_offset + mWidth, scrolled_y_pos + height, 0);
                    GL.Vertex3(x_offset, scrolled_y_pos + height, 0);

                    scrolled_y_pos += (height + y_gap);
                }
                GL.End();

                //Draw Lines
                GL.Begin(GL.LINES);
                scrolled_y_pos = y_offset - mScrollPos.y;

                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    if ( kv.Value.GetHidden() )
                    {
                        continue;
                    }
                    graph_index++;

                    float x_step = mWidth / kv.Value.GraphFullLength();

                    float height = kv.Value.GetHeight();
                    DrawGraphGridLines(scrolled_y_pos, mWidth, height, graph_index == mMouseOverGraphIndex);

                    if (kv.Value.GraphLength() > 0)
                    {
                        foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                        {
                            GraphItDataInternal g = entry.Value;

                            float y_min = kv.Value.GetMin(entry.Key);
                            float y_max = kv.Value.GetMax(entry.Key);
                            float y_range = Mathf.Max(y_max - y_min, 0.00001f);

                            //draw the 0 line
                            if (y_max > 0.0f && y_min < 0.0f)
                            {
                                GL.Color(g.mColor * 0.5f);
                                float y = scrolled_y_pos + height * (1 - (0.0f - y_min) / y_range);
                                Plot(x_offset, y, x_offset + mWidth, y);
                            }

                            GL.Color(g.mColor);
                            float previous_value = 0;
                            int start_index = (kv.Value.mCurrentIndex) % kv.Value.GraphLength();

                            for (int i = 0; i < kv.Value.GraphLength(); ++i)
                            {
                                float value = g.mDataPoints[start_index];
                                if (i >= 1)
                                {
                                    float x0 = x_offset + (i - 1) * x_step;
                                    float y0 = scrolled_y_pos + height * (1 - (previous_value - y_min) / y_range);

                                    float x1 = x_offset + i * x_step;
                                    float y1 = scrolled_y_pos + height * (1 - (value - y_min) / y_range);
                                    //Plot(x0, y0, x1, y1);
                                    DrawDataRect(x0, y0, x1, y1, scrolled_y_pos+height);
                                }
                                previous_value = value;
                                start_index = (start_index + 1) % kv.Value.GraphFullLength();
                            }
                        }
                    }

                    scrolled_y_pos += (height + y_gap);
                }                
                GL.End();

                GL.PopMatrix();
            
                GL.Viewport(new Rect(0, 0, rect.width, rect.height));
                GL.LoadPixelMatrix(0, rect.width, rect.height, 0);
            }

            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, GUIStyle.none);
            graph_index = 0;
            if (Event.current.type == EventType.Repaint)
            {
                mMouseOverGraphIndex = -1; //clear it out every repaint to ensure when the mouse leaves we don't leave the pointer around
            }
            foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
            {
                if (kv.Value.GetHidden())
                {
                    continue;
                }
                graph_index++;
                
                float height = kv.Value.GetHeight();

                GUIStyle s = new GUIStyle();                
                s.fixedHeight = height + y_gap;
                s.stretchWidth = true;
                Rect r = EditorGUILayout.BeginVertical(s);
                if (r.width != -0)
                {
                    mWidth = r.width - 2 * x_offset;
                }

                //Determine if we can fit all of the text
                float row_size = 18;
                float text_block_size = row_size * 4;
                if (kv.Value.mData.Count == 1)
                {
                    text_block_size = row_size * 3;
                }
                bool show_full_text = (kv.Value.mData.Count * text_block_size + row_size) < height;

                string num_format = "###,###,###,##0.";
                for( int i = 0; i < precision_slider; i++ )
                {
                    num_format += "#";
                }

                string fu_str = " " + (kv.Value.mFixedUpdate ? "(FixedUpdate)" : "");

                //skip subgraph title if only one, and it's the same.
                if (show_full_text)
                {
                    NameLabel.normal.textColor = Color.white;
                    EditorGUILayout.LabelField(kv.Key + fu_str, NameLabel);
                }

                foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                {
                    GraphItDataInternal g = entry.Value;
                    if (show_full_text)
                    {
                        if (kv.Value.mData.Count > 1 || entry.Key != GraphIt.BASE_GRAPH)
                        {
                            NameLabel.normal.textColor = g.mColor;
                            EditorGUILayout.LabelField(entry.Key, NameLabel);
                        }
                        EditorGUILayout.LabelField("Avg: " + g.mAvg.ToString(num_format) + " (" + g.mFastAvg.ToString(num_format) + ")", SmallLabel);
                        EditorGUILayout.LabelField("Min: " + g.mMin.ToString(num_format), SmallLabel);
                        EditorGUILayout.LabelField("Max: " + g.mMax.ToString(num_format), SmallLabel);
                    }
                    else
                    {
                        //fit each line manually or drop it
                        height -= row_size;
                        if (height >= 0)
                        {
                            if (kv.Value.mData.Count > 1)
                            {
                                NameLabel.normal.textColor = g.mColor;
                            }
                            else
                            {
                                NameLabel.normal.textColor = Color.white;
                            }
                            string text = entry.Key;
                            if( text == GraphIt.BASE_GRAPH )
                            {
                                text = kv.Key;
                            }
                            EditorGUILayout.LabelField(text + fu_str, NameLabel);
                        }
                        height -= row_size;
                        if (height >= 0)
                        {
                            EditorGUILayout.LabelField("Avg: " + g.mAvg.ToString(num_format) + " (" + g.mFastAvg.ToString(num_format) +
                            ")  Min: " + g.mMin.ToString(num_format) +
                            "  Max: " + g.mMax.ToString(num_format)
                            , SmallLabel);
                        }
                    }
                }

                //Respond to mouse input!
                if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition - Event.current.delta))
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                    {
                       // kv.Value.DoHeightDelta(Event.current.delta.y);
                    }
                }
                else if (Event.current.type != EventType.Layout && r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        mMouseOverGraphIndex = graph_index;
                        mMouseX = Event.current.mousePosition.x;

                        float x_step = mWidth / kv.Value.GraphFullLength();
                        float hover_y_offset = 0;
                        if (kv.Value.GraphLength() > 0)
                        {
                            foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                            {
                                GraphItDataInternal g = entry.Value;

                                //walk through the data points to find the correct index matching the mouse position value
                                //potential optimization here to find the index algebraically.
                                int start_index = (kv.Value.mCurrentIndex) % kv.Value.GraphLength();
                                for (int i = 0; i < kv.Value.GraphLength(); ++i)
                                {
                                    float value = g.mDataPoints[start_index];
                                    if (i >= 1)
                                    {
                                        float x0 = x_offset + (i - 1) * x_step;
                                        float x1 = x_offset + i * x_step;
                                        if (x0 < mMouseX && mMouseX <= x1)
                                        {
                                            //found this mouse positions step
                                            string text = value.ToString(num_format);

                                            Rect tooltip_r = new Rect(Event.current.mousePosition - new Vector2(250, 2 - hover_y_offset), new Vector2(250, 20));
                                            HoverText.normal.textColor = g.mColor;
                                            GUI.Label(tooltip_r, text, HoverText);

                                            hover_y_offset += 13;
                                            break;
                                        }
                                    }
                                    start_index = (start_index + 1) % kv.Value.GraphFullLength();
                                }
                            }
                        }
                    }

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                    {
                        //kv.Value.SetHidden(true);
                        //window.Repaint();
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}