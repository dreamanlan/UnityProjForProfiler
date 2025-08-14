input("*.fbx","*.FBX")
{
    int("triangleCount", 1000);
    stringlist("filter", "");
    stringlist("notfilter", "");
    stringlist("meshfilter", "");
    stringlist("meshnotfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Find Fbx Mesh");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = collectmeshes($v0, true);
        looplist($v1){
            $mesh = $$;
            $name = $mesh.name;
            $vertexCount = $mesh.vertexCount;
            $triangleCount = $mesh.triangles.Length/3;
            if(stringcontains($name, meshfilter) && stringnotcontains($name, meshnotfilter) && $triangleCount>=triangleCount){
                $v2 = newitem();
                $v2.AssetPath = assetpath;
                $v2.Info = format("mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                $v2.Order = $triangleCount;
                $v2.Value = $triangleCount;
            };
        };
        unloadasset($v0);
    };
    0;
};