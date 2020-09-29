//Tabela

using System;

public class PageTable<T>
{
    public PageElement[] Pages;

    public int pageSize;

    public PageTable(int pageCount, int pageSize)
    {
        Pages = new PageElement[pageCount];
        this.pageSize = pageSize;
    }

    public Address Insert(T element)
    {
        for (int i = 0; i < Pages.Length; i++)
        {
            if (Pages[i] == null)
            {
                Pages[i] = new PageElement
                {
                    Element = element,
                    index = 0,
                    Next = null
                };
                return new Address
                {
                    Page = i,
                    Line = 0
                };
            }
            else if (!IsFull(Pages[i]))
            {
                Pages[i] = new PageElement
                {
                    Element = element,
                    index = Pages[i].index+1,
                    Next = Pages[i]
                };
                return new Address
                {
                    Page = i,
                    Line = Pages[i].index
                };
            }
        }
        throw new Exception("PageTable is full");
    }

    public PageElement Retrieve(Address address)
    {
        if (address.Page > Pages.Length ||
            address.Line >= pageSize)
            throw new ArgumentException("Invalid page address");

        return Pages[address.Page][address.Line];
    }
    
    public PageElement Retrieve(int page, int line)
    {
        if (page > Pages.Length ||
            line >= pageSize)
            throw new ArgumentException("Invalid page address");

        return Pages[page][line];
    }
    
    public bool IsFull(PageElement page)
    {
        return page.index + 1 >= pageSize;
    }
    
    public class PageElement
    {
        public T Element;
        public int index;
        public PageElement Next;
    
        public PageElement this[int index] => index > 0 ? Next?[index - 1] : this;
    }
}

public struct Address
{
    public int Page;
    public int Line;
}

