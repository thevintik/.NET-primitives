Usage:
```cs
private async Task<object> GetResourceInLoop(ResourcePool<object> pool)
{
	const Int32 pollingIntervalInMilliseconds = 5000;
	while (true)
	{
		using (var slot = await pool.TakeSlot())
		{
			if (ConditionsAreSuitable())
			{
				slot.Reserve();
				return slot.Resource;
			}
		}
		await Task.Delay(pollingIntervalInMilliseconds);
	}
}
```