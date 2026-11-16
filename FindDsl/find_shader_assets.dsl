input("t:shader","t:visualeffect")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "allassets");
	feature("menu", "4.All Assets/Shaders");
	feature("description", "just so so");
}
filter
{
	if(asset_path.Contains(filter)){
		$v0 = load_asset(asset_path);
		order = get_shader_property_count($v0);
		info = format("{0} guid:{1} property count:{2} texture count:{3}", get_file_name(asset_path), asset_path_2_guid(asset_path), order, get_shader_property_count($v0, 4));
		1;
	}else{
		0;
	};
};