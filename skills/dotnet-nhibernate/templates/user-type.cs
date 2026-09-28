// TEMPLATE — IUserType skeleton with the interface members implemented correctly.
// See reference/custom-user-types.md before filling this in — the equality/mutability
// semantics here are easy to get subtly wrong and the mistakes don't throw immediately.

using System;
using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

/// <summary>
/// Maps {DomainType} to a single {SqlColumnType} column.
/// Replace {DomainType} and the SqlTypes/NullSafeGet/NullSafeSet bodies for your actual type.
/// </summary>
public sealed class {DomainType}UserType : IUserType
{
    // Declare the ACTUAL underlying column type precisely — a mismatch here can cause
    // silent truncation or provider-specific translation issues. Don't guess from the C# type.
    public SqlType[] SqlTypes => new[] { SqlTypeFactory.GetString(100) }; // EXAMPLE — adjust length/type

    public Type ReturnedType => typeof({DomainType});

    // If {DomainType} is an immutable value object (it should be, for most value objects),
    // set this to false — lets NHibernate skip defensive copying. If it's actually mutable,
    // this MUST be true or dirty-checking will miss real changes.
    public bool IsMutable => false;

    // Value equality, NOT reference equality — this feeds NHibernate's dirty-checking.
    // Getting this wrong causes intermittent, hard-to-reproduce "why did this UPDATE fire"
    // or "why didn't this UPDATE fire" bugs, not an immediate exception.
    public new bool Equals(object x, object y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Equals(y); // assumes {DomainType} implements value-based Equals itself
    }

    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;

    // For an immutable type, returning the value itself is usually fine.
    // For a mutable type, this MUST produce a genuine independent copy or
    // dirty-checking snapshots will alias the live object.
    public object DeepCopy(object value) => value;

    public object Disassemble(object value) => DeepCopy(value);
    public object Assemble(object cached, object owner) => DeepCopy(cached);
    public object Replace(object original, object target, object owner) => DeepCopy(original);

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var index = rs.GetOrdinal(names[0]);
        if (rs.IsDBNull(index)) // MUST check for DB null explicitly — a naive Get throws on legitimately nullable columns
            return null;

        var raw = rs.GetString(index); // adjust to the real underlying type
        return {DomainType}.Parse(raw); // replace with your actual construction logic
    }

    public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
    {
        var parameter = (IDbDataParameter)cmd.Parameters[index];
        if (value is null)
        {
            parameter.Value = DBNull.Value;
            return;
        }
        parameter.Value = value.ToString(); // replace with your actual serialization logic
    }
}
