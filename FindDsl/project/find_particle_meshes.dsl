input("*.prefab")
{
    int("totalTriangleCount", 1000);
    int("triangleCount", 200);
    bool("onlyParticle", false);
    stringlist("filter", "");
    stringlist("notfilter", "");
    stringlist("meshfilter", "");
    stringlist("meshnotfilter", "");
    float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Find Particle Meshes");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = collectprefabinfo($v0);
        //unloadasset($v0);
        $totalTriangleCount = $v1.triangleCount;
        if($totalTriangleCount>=totalTriangleCount){
            looplist($v1.meshes){
                $meshInfo = $$;
                $name = $meshInfo.meshName;
                $count = $meshInfo.meshCount;
                $vertexCount = $meshInfo.vertexCount;
                $triangleCount = $meshInfo.triangleCount;
                $tvc = $meshInfo.totalVertexCount;
                $ttc = $meshInfo.totalTriangleCount;
                $isps = $meshInfo.isParticle;
                if(stringcontains($name, meshfilter) && stringnotcontains($name, meshnotfilter) && $ttc>=triangleCount && (!onlyParticle || $isps)){
                    $v3 = newitem();
                    $v3.AssetPath = assetpath;
                    $v3.ScenePath = getassetpath($mesh);
                    $v3.Info = format("mesh:{0} vertex:{1} triangle:{2} count:{3} total_vertex:{4} total_triangle:{5} total_prefab_triangle:{6}",$name,$vertexCount,$triangleCount,$count,$tvc,$ttc,$totalTriangleCount);
                    $v3.Order = $totalTriangleCount*100000+$ttc;
                    $v3.Value = $ttc;
                    $v3.ExtraList = newextralist($v3.ScenePath => $v3.ScenePath);
                    $v3.ExtraListClickScript = "OnClickExtraListItem";
                };
            };
        };
    };
    0;
};

script(OnClickExtraListItem)args($obj,$item)
{
    selectprojectobject($obj.Value);
};