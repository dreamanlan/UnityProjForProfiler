input("SetSpecialTextures")
{
	string("filter", "");
	feature("source", "list");
	feature("menu", "6.Tools/Set Special Textures");
	feature("description", "just so so");
}
filter
{
	$lines=call_script("GetLines",0);
	loop_list($lines){
		$v0 = $$;
		loop_list(list_all_files($v0, "*body_d.*")){
			call_script("NewItem", $$, 0);
		};
		loop_list(list_all_files($v0, "*body_n.*")){
			call_script("NewItem", $$, 0);
		};
		loop_list(list_all_files($v0, "*face_d.*")){
			call_script("NewItem", $$, 1);
		};
		loop_list(list_all_files($v0, "*face_n.*")){
			call_script("NewItem", $$, 1);
		};
	};
	$lines=call_script("GetLines",1);
	loop_list($lines){
		$v0 = $$;
		loop_list(list_all_files($v0, "*body*_d.*")){
			call_script("NewItem", $$, 0);
		};
		loop_list(list_all_files($v0, "*body*_n.*")){
			call_script("NewItem", $$, 0);
		};
	};
	$lines=call_script("GetLines",2);
	loop_list($lines){
		$v0 = $$;
		loop_list(list_all_files($v0, "*face*_d.*")){
			call_script("NewItem", $$, 1);
		};
		loop_list(list_all_files($v0, "*face*_n.*")){
			call_script("NewItem", $$, 1);
		};
	};
}
process
{
	if(order==0){
		call_script("SetTexture", 1024);
	}else{
		call_script("SetTexture", 512);
	};
};

script(GetLines)args($type)
{
	$lines = list();
	if($type==0){
		$lines = read_all_lines("texture_s_npc.txt");
	}elseif($type==1){
		$lines = read_all_lines("texture_player_body.txt");
	}elseif($type==2){
		$lines = read_all_lines("texture_player_face.txt");
	};
	return($lines);
};

script(NewItem)args($file, $type)
{
	if(!$file.EndsWith(".meta") && $file.Contains(filter)){
		$v0 = $file.Replace("\\","/");
		$v10 = load_asset($v0);
		$v11 = $v10.width;
		$v12 = $v10.height;
		$v1 = new_item();
		$v1.AssetPath = $v0;
		$v1.Importer = get_asset_importer($v0);
		$v1.Info = "w*h:"+$v11+","+$v12;
		$v1.Order = $type;
		$v1.Value = 0;
	};
};

script(SetTexture)args($maxSize)
{
	/*
	$v0 = get_default_texture_setting();
	$v0.maxTextureSize = changetype(maxSize, "int");
	set_texture_setting($v0);

	$v1 = get_texture_setting("Standalone");
	$v1.maxTextureSize = changetype(maxSize, "int");
	set_texture_setting($v1);
	*/

	$v2 = get_texture_setting("iPhone");
	$v2.overridden=true;
	$v2.maxTextureSize = $maxSize;
	set_astc_texture($v2, 8);
	set_texture_setting($v2);

	$v3 = get_texture_setting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = $maxSize;
	set_astc_texture($v3, 8);
	set_texture_setting($v3);

	update_texturedb(asset_path, importer);
};