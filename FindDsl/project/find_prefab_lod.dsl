input("*.prefab")
{
    int("triangleCount", 1000);
    int("lodTriangleCount", 1000);
    stringlist("filter", "");
    stringlist("notfilter", "");
    stringlist("meshfilter", "");
    stringlist("meshnotfilter", "");
	int("lodtype",0){
		toggle(["all","no lod","lod"],[0,1,2]);
	};
	float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Find Prefab Lod");
    feature("description", "just so so");
}
filter
{
    if(stringnotcontains(assetpath, "_Lod") && stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = getdirectoryname(assetpath);
        $v1 = getfilenamewithoutextension(assetpath);
        $v2 = $v0+"/"+$v1+"_Lod.prefab";
        if(fileexist($v2)){
            if(lodtype==0 || lodtype==2){
                $v3 = loadasset($v2);
                $v4 = collectmeshes($v3, true);
                looplist($v4){
                    $mesh = $$;
                    $name = $mesh.name;
                    $vertexCount = $mesh.vertexCount;
                    $triangleCount = $mesh.triangles.Length/3;
                    if(stringcontains($name, meshfilter) && stringnotcontains($name, meshnotfilter) && $triangleCount>=lodTriangleCount){
                        $v5 = newitem();
                        $v5.AssetPath = assetpath;
                        $v5.Info = format("lod, mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                        $v5.Order = $triangleCount;
                        $v5.Value = $triangleCount;
                    };
                };
                unloadasset($v3);
            };
        }else{
            if(lodtype==0 || lodtype==1){
                $v3 = loadasset(assetpath);
                $v4 = collectmeshes($v3, true);
                looplist($v4){
                    $mesh = $$;
                    $name = $mesh.name;
                    $vertexCount = $mesh.vertexCount;
                    $triangleCount = $mesh.triangles.Length/3;
                    if(stringcontains($name, meshfilter) && stringnotcontains($name, meshnotfilter) && $triangleCount>=triangleCount){
                        $v5 = newitem();
                        $v5.AssetPath = assetpath;
                        $v5.Info = format("no lod, mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                        $v5.Order = $triangleCount;
                        $v5.Value = $triangleCount;
                    };
                };
                unloadasset($v3);
            };
        };
    };
    0;
};