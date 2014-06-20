<?php if (have_comments()) : ?>

	<ol>
		<?php wp_list_comments(array('callback' => 't2t_comment')); ?>
	</ol>

<?php if (get_comment_pages_count() > 1 && get_option('page_comments')) : ?>
	<div class="pagination">
		<a href=""><?php previous_comments_link('&#x2190; Previous') ?></a>
		<span>|</span>
		<a href=""><?php next_comments_link('Next &#x2192;') ?></a>
	</div>
<?php 
	endif; // check for comment navigation 
	else : // or, if we don't have comments:
	endif; // end have_comments() 
?>
<?php comment_form( array('title_reply' => 'Leave a reply', 'comment_notes_after'  => '')); ?>