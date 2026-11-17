input("*.prefab")
{
	string("filter", "");
	ulong("minFileId","65536");
	int("pathwidth","480");
    feature("source", "project");
    feature("menu", "1.Project Resources/Check Prefab File ID");
	feature("description", "just so so");
}
filter
{
    if(asset_path.Contains(filter)){
        $v0 = load_asset(asset_path);
        $v1 = get_components_in_children($v0,"MonoBehaviour");
        loop_list($v1){
            $v2 = $$;
            if(is_null($v2)){
                $v99 = new_item();
                $v99.AssetPath = asset_path;
                $v99.Info = "component is null.";
            }else{
                $v3 = get_guid_and_file_id($v2);
                if($v3.Value<minFileId){
                    $v99 = new_item();
                    $v99.AssetPath = asset_path;
                    $v99.Info = format("name:{0} guid:{1} file_id:{2}", $v2.name, $v3.Key, $v3.Value);
                };
            };
        };
    };
    0;
};