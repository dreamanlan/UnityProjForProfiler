input("t:shader")
{
	string("filter", "");
	feature("source", "allassets");
	feature("menu", "5.All Assets/Shaders");
	feature("description", "just so so");
}
filter
{
  if(asset_path.Contains(filter)){
  	$v0 = load_asset(asset_path);
  	order = get_shader_property_count($v0);
  	info = format("{0} property count:{1} texture count:{2}", get_file_name(asset_path), order, get_shader_property_count($v0, 4));
    1;
  }else{
    0;
  };
}
process
{
  $v0 = load_asset(asset_path);
	add_shader_to_collection($v0);
};