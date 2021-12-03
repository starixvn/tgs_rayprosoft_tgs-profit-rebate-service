using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Models;

namespace Tgs.ProfitRebate.Core
{
    public static class QueryableExtensions
    {
        private static MethodInfo OrderByMethod(bool isDesc)
        {
            var name = isDesc ? "OrderByDescending" : "OrderBy";
            return (from x in typeof(Queryable).GetMethods()
                    where x.Name == name
                    let pars = x.GetParameters()
                    where pars.Length == 2
                    select x).Single();
        }

        public static async Task<PagedResponse<TModel>> CreatePagedResponseAsync<TModel>(this IQueryable<TModel> query,
            PageableRequest request)
        {
            var result = request.SortBy == null ? query : query.OrderBy(request.GetSortOption());

            if (request.ShowAll || request.Rows == 0)
            {
                var allResult = await result.ToArrayAsync();
                return new() { Valid = true, TotalCount = allResult.Length, Items = allResult };
            }

            var pagedResult = await result.ToPagedArrayAsync(request.Page, request.Rows);

            return new() { Valid = true, TotalCount = pagedResult.TotalCount, Items = pagedResult.Items };
        }

        public static PagedResponse<TModel> CreatePagedResponse<TModel>(this IQueryable<TModel> query,
            PageableRequest request)
        {
            var result = request.SortBy == null ? query : query.OrderBy(request.GetSortOption());

            if (request.ShowAll || request.Rows == 0)
            {
                var allResult = result.ToArray();
                return new() { Valid = true, TotalCount = allResult.Length, Items = allResult };
            }

            var pagedResult = result.ToPagedArray(request.Page, request.Rows);

            return new() { Valid = true, TotalCount = pagedResult.TotalCount, Items = pagedResult.Items };
        }

        public static Task<PagedResponse<TModel>> CreatePagedResponseAsync<TModel>(this IEnumerable<TModel> query,
            int totalCount) =>
            Task.FromResult(new PagedResponse<TModel> { Valid = true, TotalCount = totalCount, Items = query.ToArray() });

        public static PagedArray<T> ToPagedArray<T>(this IQueryable<T> query, int page, int pageSize)
        {
            var totalCount = query.Count();
            page = page <= 0 ? 0 : page - 1;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var items = query.Skip(page * pageSize).Take(pageSize).ToArray();
            return new(totalCount, items);
        }

        public static async Task<PagedArray<T>> ToPagedArrayAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            var totalCount = await query.CountAsync();
            page = page <= 0 ? 0 : page - 1;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var items = await query.Skip(page * pageSize).Take(pageSize).ToArrayAsync();
            return new(totalCount, items);
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, SortOption sortOption) =>
            !string.IsNullOrEmpty(sortOption.PropertyName)
                ? query.OrderByProperty(sortOption.PropertyName, sortOption.IsDescending)
                : query;

        public static IQueryable<TSource> OrderByProperty<TSource>(this IQueryable<TSource> source, string propertyName,
            bool descending)
        {
            var parameter = Expression.Parameter(typeof(TSource), "t");
            Expression orderByProperty = Expression.Property(parameter, propertyName);

            var lambda = Expression.Lambda(orderByProperty, parameter);

            var genericMethod = OrderByMethod(descending).MakeGenericMethod(typeof(TSource), orderByProperty.Type);

            var result = genericMethod.Invoke(null, new object[] { source, lambda });

            return (IQueryable<TSource>)result;
        }


        // Where extension for filters of any nullable type
        public static IQueryable<TSource> Where<TSource, TFilter>(this IQueryable<TSource> source, TFilter? filter,
            Expression<Func<TSource, bool>> predicate) where TFilter : struct
        {
            if (filter.HasValue)
            {
                source = source.Where(predicate);
            }

            return source;
        }

        // Where extension for string filters
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, string filter,
            Expression<Func<TSource, bool>> predicate)
        {
            if (!string.IsNullOrWhiteSpace(filter))
            {
                source = source.Where(predicate);
            }

            return source;
        }

        // Where extension for collection filters
        public static IQueryable<TSource> Where<TSource, TFilter>(this IQueryable<TSource> source,
            IEnumerable<TFilter> filter,
            Expression<Func<TSource, bool>> predicate)
        {
            if (filter != null && filter.Any())
            {
                source = source.Where(predicate);
            }

            return source;
        }

        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, bool isFiltered,
            Expression<Func<TSource, bool>> predicate)
        {
            if (isFiltered)
            {
                source = source.Where(predicate);
            }

            return source;
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, bool filter,
            Func<TSource, bool> predicate)
        {
            if (filter)
            {
                source = source.Where(predicate);
            }

            return source;
        }

        public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            var results = new ConcurrentQueue<T>();
            var tasks = source.Select(async x =>
            {
                if (await predicate(x))
                {
                    results.Enqueue(x);
                }
            });
            await Task.WhenAll(tasks);

            return results;
        }

        public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> source,
            Func<T, ValueTask<bool>> predicate)
        {
            var results = new ConcurrentQueue<T>();
            var tasks = source.Select(async x =>
            {
                if (await predicate(x))
                {
                    results.Enqueue(x);
                }
            });
            await Task.WhenAll(tasks);

            return results;
        }
    }
}