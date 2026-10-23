using System;
using Sandbox.Graphics;
using VRageMath;
using VRageRender;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private void DrawHealthBars(Vector2 screen)
        {
            for(int group=0;group<ShipHealth.Names.Length;group++)
            {
                var layout=controls.HealthBars.Bars[group];if(!layout.Enabled)continue;
                float width=(float)(layout.Width*screen.X),height=(float)(layout.Height*screen.Y),labelHeight=layout.Label?24f:0;
                float x=8+(screen.X-width-16)*(float)layout.X,y=8+(screen.Y-height-labelHeight-16)*(float)layout.Y;
                int total=health.Total(group);
                if(layout.Label)
                {
                    string text=UiText.Get(controls.Language,ShipHealth.Names[group])+(healthPreview?" [preview]":"")+": "+(!health.HasBaseline?"capture or load a baseline":total==0?"no baseline blocks":health.Counts[group,0]+"/"+total+" healthy blocks");
                    MyGuiManager.DrawString("White",text,new Vector2(x/screen.X,y/screen.Y),.45f,new Color(255,255,255,(byte)(255*layout.Opacity)),useFullClientArea:true,maxTextWidth:width/screen.X);
                }
                y+=labelHeight;var background=new RectangleF(x-2,y-2,width+4,height+4);
                MyRenderProxy.DrawSprite(Texture,ref background,null,new Color(12,22,32,(byte)(210*layout.Opacity)),0,false,false);
                if(total==0)continue;
                double offset=0;
                for(int state=0;state<5;state++)
                {
                    double portion=(double)health.Counts[group,state]/total;if(portion==0)continue;
                    uint rgb=state==0?0x3296FF:controls.Appearance.Color((int)(state==1?ChangeKind.Damaged:state==2?ChangeKind.NonFunctional:state==3?ChangeKind.Missing:ChangeKind.Unknown));
                    var segment=new RectangleF(x+(float)(width*offset),y,(float)(width*portion),height);
                    var color=new Color((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,(byte)(255*layout.Opacity));
                    MyRenderProxy.DrawSprite(Texture,ref segment,null,color,0,false,false);offset+=portion;
                }
            }
        }
    }
}
