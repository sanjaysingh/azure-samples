1. max batch size of 100 is allowed
2. Batch insert only allows for entities with same partition keys
3. table delete can take up to 50 seconds, so quickly doing create after delete of a table fails with 409 error code saying it is a conflict.
4. retry policy does not work correctly. you can iplement your own retry.