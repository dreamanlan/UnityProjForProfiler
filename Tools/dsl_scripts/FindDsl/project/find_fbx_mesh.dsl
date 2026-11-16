input("*.fbx","*.FBX")
{
    int("triangleCount", 1000);
    int("componentCount", 3);
    stringlist("filter", "");
    stringlist("notfilter", "");
    stringlist("uvfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Check Fbx Mesh");
    feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = load_asset(asset_path);
        $v1 = collect_mesh_info($v0, importer);
        order = $v1.triangleCount;
        if($v1.triangleCount >= triangleCount){
            $v2 = calc_mesh_vertex_component_count($v0,true);
            loop_list($v2){
                $key = $$.Key;
                $val = $$.Value;
                if($val >= componentCount && string_contains($key, uvfilter)){
                    $v3 = new_item();
                    $v3.AssetPath = asset_path;
                    $v3.Info = format("skinned:{0},mesh:{1},vertex:{2},triangle:{3},vertex components:{4} {5}",
                        $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $val, $key
                        );
                    $v3.Order = $val;
                    $v3.Value = 0;
                };
            };
        };
        unload_asset($v0);
    };
    0;
};