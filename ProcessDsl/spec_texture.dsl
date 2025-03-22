input("SetSpecialTextures")
{
	string("filter", "");
	feature("source", "list");
	feature("menu", "6.Tools/Set Special Textures");
	feature("description", "just so so");
}
filter
{
	$lines=callscript("GetLines",0);
	looplist($lines){
		$v0 = $$;
		looplist(listallfiles($v0, "*body_d.*")){
			callscript("NewItem", $$, 0);
		};
		looplist(listallfiles($v0, "*body_n.*")){
			callscript("NewItem", $$, 0);
		};
		looplist(listallfiles($v0, "*face_d.*")){
			callscript("NewItem", $$, 1);
		};
		looplist(listallfiles($v0, "*face_n.*")){
			callscript("NewItem", $$, 1);
		};
	};
	$lines=callscript("GetLines",1);
	looplist($lines){
		$v0 = $$;
		looplist(listallfiles($v0, "*body*_d.*")){
			callscript("NewItem", $$, 0);
		};
		looplist(listallfiles($v0, "*body*_n.*")){
			callscript("NewItem", $$, 0);
		};
	};
	$lines=callscript("GetLines",2);
	looplist($lines){
		$v0 = $$;
		looplist(listallfiles($v0, "*face*_d.*")){
			callscript("NewItem", $$, 1);
		};
		looplist(listallfiles($v0, "*face*_n.*")){
			callscript("NewItem", $$, 1);
		};
	};
}
process
{
	if(order==0){
		callscript("SetTexture", 1024);
	}else{
		callscript("SetTexture", 512);
	};
};

script(GetLines)args($type)
{
	$lines = list();
	if($type==0){
		$lines = readalllines("texture_s_npc.txt");
	}elseif($type==1){
		$lines = readalllines("texture_player_body.txt");
	}elseif($type==2){
		$lines = readalllines("texture_player_face.txt");
	};
	return($lines);
};

script(NewItem)args($file, $type)
{
	if(!$file.EndsWith(".meta") && $file.Contains(filter)){
		$v0 = $file.Replace("\\","/");
		$v10 = loadasset($v0);
		$v11 = $v10.width;
		$v12 = $v10.height;
		$v1 = newitem();
		$v1.AssetPath = $v0;
		$v1.Importer = getassetimporter($v0);
		$v1.Info = "w*h:"+$v11+","+$v12;
		$v1.Order = $type;
		$v1.Value = 0;
	};
};

script(SetTexture)args($maxSize)
{
	/*
	$v0 = getdefaulttexturesetting();
	$v0.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v0);

	$v1 = gettexturesetting("Standalone");
	$v1.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v1);
	*/

	$v2 = gettexturesetting("iPhone");
	$v2.overridden=true;
	$v2.maxTextureSize = $maxSize;
	setastctexture($v2, 8);
	settexturesetting($v2);

	$v3 = gettexturesetting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = $maxSize;
	setastctexture($v3, 8);
	settexturesetting($v3);

	updatetexturedb(assetpath, importer);
};