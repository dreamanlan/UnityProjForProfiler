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
    if(string_not_contains(asset_path, "_Lod") && string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = get_directory_name(asset_path);
        $v1 = get_file_name_without_extension(asset_path);
        $v2 = $v0+"/"+$v1+"_Lod.prefab";
        if(file_exist($v2)){
            if(lodtype==0 || lodtype==2){
                $v3 = load_asset($v2);
                $v4 = collect_meshes($v3, true);
                loop_list($v4){
                    $mesh = $$;
                    $name = $mesh.name;
                    $vertexCount = $mesh.vertexCount;
                    $triangleCount = $mesh.triangles.Length/3;
                    if(string_contains($name, meshfilter) && string_not_contains($name, meshnotfilter) && $triangleCount>=lodTriangleCount){
                        $v5 = new_item();
                        $v5.AssetPath = asset_path;
                        $v5.Info = format("lod, mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                        $v5.Order = $triangleCount;
                        $v5.Value = $triangleCount;
                    };
                };
                unload_asset($v3);
            };
        }else{
            if(lodtype==0 || lodtype==1){
                $v3 = load_asset(asset_path);
                $v4 = collect_meshes($v3, true);
                loop_list($v4){
                    $mesh = $$;
                    $name = $mesh.name;
                    $vertexCount = $mesh.vertexCount;
                    $triangleCount = $mesh.triangles.Length/3;
                    if(string_contains($name, meshfilter) && string_not_contains($name, meshnotfilter) && $triangleCount>=triangleCount){
                        $v5 = new_item();
                        $v5.AssetPath = asset_path;
                        $v5.Info = format("no lod, mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                        $v5.Order = $triangleCount;
                        $v5.Value = $triangleCount;
                    };
                };
                unload_asset($v3);
            };
        };
    };
    0;
};