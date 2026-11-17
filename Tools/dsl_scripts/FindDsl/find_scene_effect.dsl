input("*.prefab")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneassets");
	feature("menu", "2.Current Scene Resources/Effects");
	feature("description", "just so so");
}
filter
{
	object = load_asset(asset_path);
	$v0 = get_components_in_children(object,"ParticleSystem");
	order = $v0.Length;
	if($v0.Length > 0 && asset_path.Contains(filter)){
		info = format("particle system count:{0}", $v0.Length);
		1;
	}else{
		0;
	};
};