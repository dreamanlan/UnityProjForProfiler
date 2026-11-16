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
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = load_asset(asset_path);
        $v1 = collect_meshes($v0, true);
        loop_list($v1){
            $mesh = $$;
            $name = $mesh.name;
            $vertexCount = $mesh.vertexCount;
            $triangleCount = $mesh.triangles.Length/3;
            if(string_contains($name, meshfilter) && string_not_contains($name, meshnotfilter) && $triangleCount>=triangleCount){
                $v2 = new_item();
                $v2.AssetPath = asset_path;
                $v2.Info = format("mesh:{0} vertex:{1} triangle:{2}",$name,$vertexCount,$triangleCount);
                $v2.Order = $triangleCount;
                $v2.Value = $triangleCount;
            };
        };
        unload_asset($v0);
    };
    0;
};