using System;
using Marigold;

internal static class ControlChecks
{
    public static void Run()
    {
        int count = 0;
        Action<bool> check = value => { if (!value) throw new Exception("Command check failed: " + count); count++; };
        check(!SchematicControls.Owns(null));
        check(!SchematicControls.Owns("hello"));
        check(!SchematicControls.Owns("/m-other hide"));
        check(!SchematicControls.Owns("/msg player hello"));
        check(SchematicControls.Owns("/marigold hide"));
        check(!SchematicControls.Owns("/m player"));
        check(!SchematicControls.Owns("/mg-other hide"));
        check(SchematicControls.Owns(" /MG\tshow "));
        var c = new SchematicControls();
        check(c.Execute("/mg") == SchematicControls.Help);
        c.Execute("/mg hide"); check(!c.Visible);
        c.Execute("/mg show"); check(c.Visible);
        c.Execute("/mg spin"); check(!c.Spin);
        c.Execute("/mg spin"); check(c.Spin);
        c.Execute("/mg left"); check(!c.Spin && c.Yaw == Math.PI/2 && c.Pitch == 0);
        c.Execute("/mg right"); check(c.Yaw == -Math.PI/2 && c.Pitch == 0);
        c.Execute("/mg top"); check(c.Pitch == -Math.PI/2 && c.Yaw == Math.PI);
        c.Execute("/mg bottom"); check(c.Pitch == Math.PI/2 && c.Yaw == 0);
        c.Execute("/mg zoom 2"); check(c.Zoom == 2);
        foreach (string invalid in new[]{"NaN","Infinity","-1","4","oops"})
        { c.Execute("/mg zoom " + invalid); check(c.Zoom == 2); }
        for (int i=0;i<40;i++) c.Execute("/mg in"); check(c.Zoom == 3);
        for (int i=0;i<40;i++) c.Execute("/mg out"); check(c.Zoom == 0.25);
        check(c.Execute("/mg hide extra") == SchematicControls.Help && c.Visible);
        check(c.Execute("/mg unknown") == SchematicControls.Help);
        c.Execute("/mg reset"); check(c.Zoom == 0.85 && c.Yaw == 0 && c.Pitch == -0.4 && c.Spin);
        string response; bool send = true;
        check(c.TryHandle("/mg hide",ref send,out response) && !send && !c.Visible);
        send=true;
        check(c.TryHandle("/mg invalid",ref send,out response) && !send);
        send=true;
        check(!c.TryHandle("hello",ref send,out response) && send && response==null);
        send=false;
        check(!c.TryHandle("/another-plugin",ref send,out response) && !send);
        c.Execute("/mg zoom 2");
        c.Execute("/mg left"); c.Execute("/mg left");
        check(c.Yaw == Math.PI/2 && c.Pitch == 0 && c.Zoom == 2 && !c.Spin);
        c.Execute("/mg top");
        var nose = SchematicGeometry.Rotate(new Point3(0,0,-1),c.Yaw,c.Pitch);
        check(nose.Y > 0.999);
        c.Execute("/mg bottom");
        nose = SchematicGeometry.Rotate(new Point3(0,0,-1),c.Yaw,c.Pitch);
        check(nose.Y > 0.999);
        var axes = new[]{new Point3(1,0,0),new Point3(-1,0,0),new Point3(0,1,0),new Point3(0,-1,0),new Point3(0,0,1),new Point3(0,0,-1)};
        foreach(var right in axes) foreach(var up in axes)
        {
            if(Math.Abs(right.X*up.X+right.Y*up.Y+right.Z*up.Z)>0.1)continue;
            var back = new Point3(right.Y*up.Z-right.Z*up.Y,right.Z*up.X-right.X*up.Z,right.X*up.Y-right.Y*up.X);
            var p = new Point3(0.2*right.X+0.3*up.X-0.7*back.X,0.2*right.Y+0.3*up.Y-0.7*back.Y,0.2*right.Z+0.3*up.Z-0.7*back.Z);
            var local = SchematicGeometry.ToBasis(p,right,up,back);
            check(Math.Abs(local.X-0.2)<1e-9 && Math.Abs(local.Y-0.3)<1e-9 && Math.Abs(local.Z+0.7)<1e-9);
            foreach(var defaultView in new[]{new SchematicControls(),c})
            {
                if(ReferenceEquals(defaultView,c)) { defaultView.Execute("/mg bottom"); defaultView.Execute("/mg reset"); }
                foreach(int rear in new[]{-10,10}) foreach(int side in new[]{-2,2})
                {
                    var landmark=new Point3(side*right.X-up.X+rear*back.X,side*right.Y-up.Y+rear*back.Y,side*right.Z-up.Z+rear*back.Z);
                    var projected=defaultView.Project(SchematicGeometry.ToBasis(landmark,right,up,back));
                    check(Math.Abs(projected.X+side)<1e-9);
                }
            }
            foreach(string view in new[]{"left","right","top","bottom"})
            {
                c.Execute("/mg " + view);
                if(view=="top" || view=="bottom")
                {
                    // An asymmetric rear-port drill must remain lower-left in either plan view.
                    var drill=new Point3(-2*right.X+3*back.X,-2*right.Y+3*back.Y,-2*right.Z+3*back.Z);
                    var displayed=c.Project(SchematicGeometry.ToBasis(drill,right,up,back));
                    check(displayed.X < -1.999 && displayed.Y < -2.999);
                }
                var direction = view=="left" ? new Point3(-1,0,0) : view=="right" ? new Point3(1,0,0) : view=="top" ? new Point3(0,1,0) : new Point3(0,-1,0);
                var gridDirection = new Point3(direction.X*right.X+direction.Y*up.X+direction.Z*back.X,direction.X*right.Y+direction.Y*up.Y+direction.Z*back.Y,direction.X*right.Z+direction.Y*up.Z+direction.Z*back.Z);
                var camera = SchematicGeometry.Rotate(SchematicGeometry.ToBasis(gridDirection,right,up,back),c.Yaw,c.Pitch);
                check(Math.Abs(camera.X)<1e-9 && Math.Abs(camera.Y)<1e-9 && ((view=="top" || view=="bottom") ? camera.Z < -0.999 : camera.Z > 0.999) && !c.Spin);
            }
        }
        c.Execute("/mg reset");
        foreach(double yaw in new[]{0.0,0.6,1.7,3.2,5.4})
        {
            c.Yaw=yaw;
            var point=new Point3(-2,-1,3);
            var raw=SchematicGeometry.Rotate(point,c.Yaw,c.Pitch);
            var shown=c.Project(point);
            check(Math.Abs(shown.X+raw.X)<1e-9 && shown.Y==raw.Y && shown.Z==raw.Z);
        }
        var beforePause=c.Project(new Point3(-2,-1,3));
        c.Execute("/mg spin");
        check(!c.Spin && Math.Abs(c.Yaw-Math.PI/4)<1e-9 && Math.Abs(c.Pitch+Math.Atan(1/Math.Sqrt(2)))<1e-9);
        c.Execute("/mg top");
        check(c.Project(new Point3(-2,-1,3)).X==-2);
        c.Execute("/mg spin");
        var resumed=c.Project(new Point3(-2,-1,3));
        var resumedRaw=SchematicGeometry.Rotate(new Point3(-2,-1,3),c.Yaw,c.Pitch);
        check(Math.Abs(resumed.X+resumedRaw.X)<1e-9);
        Console.WriteLine("PASS: " + count + " command/orientation checks.");
    }
}

