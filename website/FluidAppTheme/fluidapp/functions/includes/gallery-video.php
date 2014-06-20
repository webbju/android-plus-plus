<?php 
if(file_exists('../../../../wp-load.php')) :
	include '../../../../wp-load.php';
else:
	include '../../../../../wp-load.php';
endif; 

?>
<div class="gallery_video">
	<?php echo stripslashes(htmlspecialchars_decode(get_post_meta($_GET['id'], '_embed_code', true))); ?>
</div>