input("*.prefab")
{
    int("totalTriangleCount", 1000);
    int("triangleCount", 200);
    int("subMeshCount", 1);
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
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = load_asset(asset_path);
        $v1 = collect_prefab_info($v0);
        $totalTriangleCount = $v1.triangleCount;
        if($totalTriangleCount>=totalTriangleCount){
            loop_list($v1.meshes){
                $meshInfo = $$;
                $name = $meshInfo.meshName;
                $count = $meshInfo.meshCount;
                $subMeshCount = $meshInfo.subMeshCount;
                $vertexCount = $meshInfo.vertexCount;
                $triangleCount = $meshInfo.triangleCount;
                $tvc = $meshInfo.totalVertexCount;
                $ttc = $meshInfo.totalTriangleCount;
                $isps = $meshInfo.isParticle;
                if(string_contains($name, meshfilter) && string_not_contains($name, meshnotfilter) && $ttc>=triangleCount && $subMeshCount>=subMeshCount && (!onlyParticle || $isps)){
                    $v3 = new_item();
                    $v3.AssetPath = asset_path;
                    $v3.ScenePath = get_asset_path($meshInfo.mesh);
                    $v3.Info = format("mesh:{0} vertex:{1} triangle:{2} count:{3} total_vertex:{4} total_triangle:{5} total_prefab_triangle:{6} sub_mesh_count:{7}",$name,$vertexCount,$triangleCount,$count,$tvc,$ttc,$totalTriangleCount,$subMeshCount);
                    $v3.Order = $totalTriangleCount*100000+$ttc;
                    $v3.Value = $ttc;
                    $v3.ExtraList = new_extra_list($v3.ScenePath => $v3.ScenePath);
                    $v3.ExtraListClickScript = "OnClickExtraListItem";
                };
            };
        };
        unload_asset($v0);
    };
    0;
};

script(OnClickExtraListItem)args($obj,$item)
{
    select_project_object($obj.Value);
};