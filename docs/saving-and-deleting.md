This section refers to the saving and deleting of entities. For bulk updates and deletes see the page on [Bulk Queries](bulk-queries).
Bulk queries can also be used to update and delete entities but without the cost of selecting the entity from the database in the first place.

Saving New Entities
-------------------

Saving a new entity is as simple as creating the entity and then calling InsertAsync on the session e.g.

	using (var session = database.BeginSession()) {
		var post = new Post { Title = "...", Content = "..." };
		await session.InsertAsync(post);
		session.Complete();
	}

There are several things to note here:

* The call to InsertAsync actually executes the Insert statement at that moment, but does not commit the statement until the transaction is commited at `session.Complete()`
* By default integer primary keys are created with Identity/Autoincrement semantics and calling `InsertAsync()` sets the Id to the database generated value
* Related entities are not automatically saved for you i.e. there is no cascading save implemented. You have to explicitly insert all of the associated data as well

Updating an Entity
------------------

Updating an entity is as simple as fetching the entity and then saving it. Dashing employs change tracking
on the entities in order to only update those columns that have changed.

	using (var session = database.BeginSession()) {
		var post = await session.GetAsync<Post>(1);
		post.Content = "...";
		await session.SaveAsync(post);
		session.Complete();
	}
	
A note to EF users: our approach to updating entities differs in that you have to explicitly call
SaveAsync on an updated entity. The entity itself records whether properties have changed so Dashing
simply inspects the entity for changes and then executes the update statement (or not if there are no
changes) at that point.

Deleting an Entity
------------------

Deleting an entity is as simple as fetching the entity and then deleting it.

	using (var session = database.BeginSession()) {
		var post = await session.GetAsync<Post>(1);
		session.DeleteAsync(post);
		session.Complete();
	}
	
If you don't need to get the entity in order to delete it (which, let's be honest, seems slightly non-performant)
please refer to the [Bulk Queries](bulk-queries) on how to execute a delete statement in a strongly typed way.